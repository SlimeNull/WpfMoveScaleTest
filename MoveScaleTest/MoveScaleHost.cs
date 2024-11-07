using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MoveScaleTest
{
    [TemplatePart(Name = "ContentHost", Type = typeof(FrameworkElement))]
    public class MoveScaleHost : ContentControl
    {
        static MoveScaleHost()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MoveScaleHost), new FrameworkPropertyMetadata(typeof(MoveScaleHost)));
        }

        private bool _isMoving;
        private Vector _startOffset;
        private Point _startMousePosition;
        private FrameworkElement? _cachedContentHost;
        private AnimationTimeline? _runningOffsetAnimation;
        private AnimationTimeline? _runningScaleAnimation;
        private Vector _animationTargetOffset;
        private double _animationTargetScale;

        private FrameworkElement? ContentHost => _cachedContentHost ??= GetTemplateChild("ContentHost") as FrameworkElement;

        private double ClientWidth => ActualWidth - BorderThickness.Left - BorderThickness.Right - Padding.Left - Padding.Right;
        private double ClientHeight => ActualHeight - BorderThickness.Top - BorderThickness.Bottom - Padding.Top - Padding.Bottom;

        public bool CanAdjust
        {
            get { return (bool)GetValue(CanAdjustProperty); }
            set { SetValue(CanAdjustProperty, value); }
        }

        public Vector Offset
        {
            get { return (Vector)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        public double MaxOffsetDistance
        {
            get { return (double)GetValue(MaxOffsetDistanceProperty); }
            set { SetValue(MaxOffsetDistanceProperty, value); }
        }

        public double MinScale
        {
            get { return (double)GetValue(MinScaleProperty); }
            set { SetValue(MinScaleProperty, value); }
        }

        public double MaxScale
        {
            get { return (double)GetValue(MaxScaleProperty); }
            set { SetValue(MaxScaleProperty, value); }
        }

        public Point OriginPoint
        {
            get { return (Point)GetValue(OriginPointProperty); }
            set { SetValue(OriginPointProperty, value); }
        }

        public IEasingFunction EasingFunction
        {
            get { return (IEasingFunction)GetValue(EasingFunctionProperty); }
            set { SetValue(EasingFunctionProperty, value); }
        }

        public Duration EasingDuration
        {
            get { return (Duration)GetValue(EasingDurationProperty); }
            set { SetValue(EasingDurationProperty, value); }
        }


        public static readonly DependencyProperty CanAdjustProperty =
            DependencyProperty.Register(nameof(CanAdjust), typeof(bool), typeof(MoveScaleHost), new FrameworkPropertyMetadata(true));

        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.Register(nameof(Offset), typeof(Vector), typeof(MoveScaleHost), new FrameworkPropertyMetadata(default(Vector), propertyChangedCallback: OnOffsetOrScaleChanged, coerceValueCallback: CoerceOffset));

        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register(nameof(Scale), typeof(double), typeof(MoveScaleHost), new FrameworkPropertyMetadata(1.0, propertyChangedCallback: OnOffsetOrScaleChanged, coerceValueCallback: CoerceScale));

        public static readonly DependencyProperty MaxOffsetDistanceProperty =
            DependencyProperty.Register(nameof(MaxOffsetDistance), typeof(double), typeof(MoveScaleHost), new FrameworkPropertyMetadata(double.NaN, propertyChangedCallback: OnMaximumOffsetDistanceChanged));

        public static readonly DependencyProperty MaxScaleProperty =
            DependencyProperty.Register(nameof(MaxScale), typeof(double), typeof(MoveScaleHost), new FrameworkPropertyMetadata(double.NaN, propertyChangedCallback: OnMaximumScaleChanged));

        public static readonly DependencyProperty MinScaleProperty =
            DependencyProperty.Register(nameof(MinScale), typeof(double), typeof(MoveScaleHost), new FrameworkPropertyMetadata(double.NaN, propertyChangedCallback: OnMinimumScaleChanged));

        public static readonly DependencyProperty OriginPointProperty =
            DependencyProperty.Register(nameof(OriginPoint), typeof(Point), typeof(MoveScaleHost), new FrameworkPropertyMetadata(new Point(0.5, 0.5)));

        public static readonly DependencyProperty EasingFunctionProperty =
            DependencyProperty.Register(nameof(EasingFunction), typeof(IEasingFunction), typeof(MoveScaleHost), new FrameworkPropertyMetadata(new SineEase() { EasingMode = EasingMode.EaseOut }));

        public static readonly DependencyProperty EasingDurationProperty =
            DependencyProperty.Register(nameof(EasingDuration), typeof(Duration), typeof(MoveScaleHost), new FrameworkPropertyMetadata(default(Duration)));


        private static void OnOffsetOrScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MoveScaleHost host)
            {
                return;
            }

            host.ApplyOffsetAndScale();
        }

        private static void OnMaximumOffsetDistanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MoveScaleHost host)
            {
                return;
            }

            if (e.NewValue is double newValue &&
                !double.IsNaN(newValue) &&
                host.Offset.LengthSquared > (newValue * newValue))
            {
                var newOffset = host.Offset;
                newOffset.Normalize();
                newOffset *= newValue;

                host.Offset = newOffset;
            }
        }

        private static void OnMaximumScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MoveScaleHost host)
            {
                return;
            }

            if (e.NewValue is double newValue &&
                !double.IsNaN(newValue) &&
                host.Scale > newValue)
            {
                host.Scale = newValue;
            }
        }

        private static void OnMinimumScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MoveScaleHost host)
            {
                return;
            }

            if (e.NewValue is double newValue &&
                !double.IsNaN(newValue) &&
                host.Scale < newValue)
            {
                host.Scale = newValue;
            }
        }

        private static object CoerceOffset(DependencyObject d, object baseValue)
        {
            if (d is not MoveScaleHost host ||
                baseValue is not Vector baseOffset)
            {
                return baseValue;
            }

            var maxOffsetDistance = host.MaxOffsetDistance;
            if (baseOffset.LengthSquared > (maxOffsetDistance * maxOffsetDistance))
            {
                var newOffset = baseOffset;
                newOffset.Normalize();
                newOffset *= maxOffsetDistance;

                return newOffset;
            }

            return baseValue;
        }

        private static object CoerceScale(DependencyObject d, object baseValue)
        {
            if (d is not MoveScaleHost host ||
                baseValue is not double baseScale)
            {
                return baseValue;
            }

            var minScale = host.MinScale;
            var maxScale = host.MaxScale;
            if (baseScale < minScale)
            {
                return minScale;
            }
            else if (baseScale > maxScale)
            {
                return maxScale;
            }

            return baseValue;
        }

        void StartMove()
        {
            _isMoving = CaptureMouse();

            if (_isMoving)
            {
                _startOffset = Offset;
                _startMousePosition = Mouse.GetPosition(this);
            }
        }

        void StepMove()
        {
            var currentMousePosition = Mouse.GetPosition(this);
            var positionOffset = currentMousePosition - _startMousePosition;

            Offset = _startOffset + positionOffset;
        }

        void StopMove()
        {
            ReleaseMouseCapture();
            _isMoving = false;
        }

        void ApplyOffsetAndScale()
        {
            if (ContentHost is not FrameworkElement transformHost)
            {
                return;
            }

            if (transformHost.RenderTransform is TransformGroup transformGroup &&
                transformGroup.Children.Count == 2 &&
                transformGroup.Children[0] is ScaleTransform scaleTransform &&
                transformGroup.Children[1] is TranslateTransform translateTransform)
            {
                translateTransform.X = Offset.X;
                translateTransform.Y = Offset.Y;
                scaleTransform.ScaleX = Scale;
                scaleTransform.ScaleY = Scale;
            }
            else
            {
                transformHost.RenderTransform = new TransformGroup()
                {
                    Children =
                    {
                        new ScaleTransform(),
                        new TranslateTransform(),
                    }
                };

                ApplyOffsetAndScale();
            }
        }

        void CalculateScaleBy(Point normalizedCenter, double factor, out Vector newOffset, out double newScale)
        {
            newScale = Scale * factor;

            if (ContentHost is not FrameworkElement contentHost)
            {
                return;
            }

            var transformOrigin = OriginPoint;
            var normalizedOffsetFromOrigin = normalizedCenter - transformOrigin;
            var standardNormalizedOffsetDiff = 1 / factor - 1;
            var normalizedOffsetDiff = new Vector(standardNormalizedOffsetDiff * normalizedOffsetFromOrigin.X, standardNormalizedOffsetDiff * normalizedOffsetFromOrigin.Y);
            var absoluteOffsetDiff = new Vector(normalizedOffsetDiff.X * ClientWidth * newScale, normalizedOffsetDiff.Y * ClientHeight * newScale);

            newOffset = Offset + absoluteOffsetDiff;
        }

        void CalculateMakePointCenter(Point normalizedPoint, out Vector newOffset)
        {
            if (ContentHost is not FrameworkElement contentHost)
            {
                return;
            }

            var scale = Scale;
            var originPoint = OriginPoint;

            var normalizedCenter = new Point(originPoint.X + (0.5 - originPoint.X) / scale, originPoint.Y + (0.5 - originPoint.Y) / scale);
            var normalizedOffsetFromCenter = new Point(normalizedPoint.X - normalizedCenter.X, normalizedPoint.Y - normalizedCenter.Y);
            var pixelOffsetFromCenter = new Vector(normalizedOffsetFromCenter.X * ClientWidth * scale, normalizedOffsetFromCenter.Y * ClientHeight * scale);

            newOffset = -pixelOffsetFromCenter;
        }

        void AnimateOffset(Vector newOffset)
        {
            var animation = new VectorAnimation()
            {
                EasingFunction = EasingFunction,
                Duration = EasingDuration,
                From = Offset,
                To = newOffset,
                FillBehavior = FillBehavior.HoldEnd
            };

            animation.Completed += (s, e) =>
            {
                if (_runningOffsetAnimation != animation)
                {
                    return;
                }

                BeginAnimation(OffsetProperty, null);
                _runningOffsetAnimation = null;
                Offset = newOffset;
            };

            BeginAnimation(OffsetProperty, null);
            BeginAnimation(OffsetProperty, animation, HandoffBehavior.SnapshotAndReplace);

            _runningOffsetAnimation = animation;
            _animationTargetOffset = newOffset;
        }

        void AnimateScale(double newScale)
        {
            var animation = new DoubleAnimation()
            {
                EasingFunction = EasingFunction,
                Duration = EasingDuration,
                From = Scale,
                To = newScale,
                FillBehavior = FillBehavior.HoldEnd
            };

            animation.Completed += (s, e) =>
            {
                if (_runningScaleAnimation != animation)
                {
                    return;
                }

                BeginAnimation(OffsetProperty, null);
                _runningScaleAnimation = null;
                Scale = newScale;
            };

            BeginAnimation(ScaleProperty, null);
            BeginAnimation(ScaleProperty, animation, HandoffBehavior.SnapshotAndReplace);
            _runningScaleAnimation = animation;
            _animationTargetScale = newScale;
        }

        public void ScaleTo(double newScale)
        {
            if (EasingDuration.HasTimeSpan &&
                EasingDuration.TimeSpan != default)
            {
                AnimateScale(newScale);
            }
            else
            {
                Scale = newScale;
            }
        }

        public void ScaleBy(double factor)
        {
            ScaleBy(Scale * factor);
        }

        public void ScaleTo(Point normalizedCenter, double newScale)
        {
            ScaleBy(normalizedCenter, newScale / Scale);
        }

        public void ScaleBy(Point normalizedCenter, double factor)
        {
            bool isMoving = _isMoving;
            if (isMoving)
            {
                StopMove();
            }

            CalculateScaleBy(normalizedCenter, factor, out var newOffset, out var newScale);

            if (EasingDuration.HasTimeSpan &&
                EasingDuration.TimeSpan != default)
            {
                AnimateScale(newScale);
                AnimateOffset(newOffset);
            }
            else
            {
                Scale = newScale;
                Offset = newOffset;
            }

            if (isMoving)
            {
                StartMove();
            }
        }

        public void MoveTo(Vector newOffset)
        {
            if (EasingDuration.HasTimeSpan &&
                EasingDuration.TimeSpan != default)
            {
                AnimateOffset(newOffset);
            }
            else
            {
                Offset = newOffset;
            }
        }

        public void MoveBy(Vector offset)
        {
            MoveTo(Offset + offset);
        }

        public void SetOriginPoint(Point normalizedPoint)
        {
            if (ContentHost is not FrameworkElement contentHost)
            {
                return;
            }

            var scale = Scale;

            var currentOrigin = OriginPoint;
            var newOrigin = normalizedPoint;
            var originDiff = newOrigin - currentOrigin;
            var normalizedOffsetDiff = originDiff * (1 - 1 / scale);
            var offsetDiff = new Vector(normalizedOffsetDiff.X * ClientWidth * scale, normalizedOffsetDiff.Y * ClientHeight * scale);

            Offset += offsetDiff;
            OriginPoint = newOrigin;
        }

        public void SetOriginPointToMousePosition()
        {
            if (ContentHost is not FrameworkElement transformHost)
            {
                return;
            }

            var mousePosition = Mouse.GetPosition(transformHost);
            var normalizedMousePosition = new Point(mousePosition.X / transformHost.ActualWidth, mousePosition.Y / transformHost.ActualHeight);
            SetOriginPoint(normalizedMousePosition);
        }

        public void SetCenterPoint(Point normalizedPoint)
        {
            bool isMoving = _isMoving;
            if (isMoving)
            {
                StopMove();
            }

            CalculateMakePointCenter(normalizedPoint, out var newOffset);

            if (EasingDuration.HasTimeSpan &&
                EasingDuration.TimeSpan != default)
            {
                AnimateOffset(newOffset);
            }
            else
            {
                Offset = newOffset;
            }

            if (isMoving)
            {
                StartMove();
            }
        }

        public void Reset()
        {
            if (EasingDuration.HasTimeSpan &&
                EasingDuration.TimeSpan != default)
            {
                AnimateOffset(default);
                AnimateScale(1);
            }
            else
            {
                Offset = default;
                Scale = 1;
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                StartMove();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isMoving)
            {
                StepMove();

                e.Handled = true;
                return;
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (_isMoving)
            {
                StopMove();

                e.Handled = true;
                return;
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (CanAdjust &&
                e.LeftButton == MouseButtonState.Pressed &&
                ContentHost is FrameworkElement contentHost)
            {
                var relativeMousePosition = e.GetPosition(contentHost);
                var normalizedMousePosition = new Point(relativeMousePosition.X / contentHost.ActualWidth, relativeMousePosition.Y / contentHost.ActualHeight);

                if (_isMoving)
                {
                    StopMove();
                }

                SetCenterPoint(normalizedMousePosition);

                e.Handled = true;
                return;
            }

            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (CanAdjust &&
                ContentHost is FrameworkElement contentHost)
            {
                var relativeMousePosition = e.GetPosition(contentHost);
                var normalizedMousePosition = new Point(relativeMousePosition.X / contentHost.ActualWidth, relativeMousePosition.Y / contentHost.ActualHeight);

                var factor = Math.Pow(2, (double)e.Delta / Mouse.MouseWheelDeltaForOneLine);
                ScaleBy(normalizedMousePosition, factor);

                e.Handled = true;
                return;
            }

            base.OnMouseWheel(e);
        }
    }
}

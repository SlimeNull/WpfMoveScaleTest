using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace MoveScaleTest
{
    public class CheckerboardBrushExtension : MarkupExtension
    {
        public Brush? Brush1 { get; set; }
        public Brush? Brush2 { get; set; }
        public double TileSize { get; set; } = 8;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new DrawingBrush()
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, TileSize, TileSize),
                ViewportUnits = BrushMappingMode.Absolute,

                Drawing = new DrawingGroup()
                {
                    Children =
                    {
                        new GeometryDrawing()
                        {
                            Brush = Brush2,
                            Geometry = new RectangleGeometry(new Rect(0, 0, TileSize, TileSize))
                        },
                        new GeometryDrawing()
                        {
                            Brush = Brush1,
                            Geometry = new GeometryGroup()
                            {
                                Children =
                                {
                                    new RectangleGeometry(new Rect(0, 0, TileSize / 2, TileSize / 2)),
                                    new RectangleGeometry(new Rect(TileSize / 2, TileSize / 2, TileSize / 2, TileSize / 2)),
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}

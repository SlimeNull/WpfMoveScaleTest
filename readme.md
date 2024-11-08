# WpfMoveScaleTest

WPF 拖拽移动以及滚动缩放 Demo.

![](Assets/demo.webp)

### 使用方式 / Usage

1. 将此项目中的 MoveScaleHost.cs 复制到你的项目中
2. 将此项目中 Themes/Generic.xaml 中的 MoveScaleHost 的默认样式复制到你的项目中
3. 在要支持移动和缩放的地方, 放置此控件, 并将内容置于 MoveScaleHost 下


### 基本操作 / Basic Operations

- 拖拽: 移动内容
- 滚动: 缩放内容
- 双击: 将点击的点移动到视图中央
- 调用 MoveScaleHost.Reset 方法, 重置移动和缩放
namespace Gnosis.Rendering.Widget;

// 停靠布局容器，支持 Top/Bottom/Left/Right/Fill 五种停靠位置
// 测量采用三阶段策略：先测量 Top/Bottom，再测量 Left/Right，最后测量 Fill
public sealed class Dock : ContainerWidget
{
    #region Fields

    // 存储子组件的停靠位置
    private readonly Dictionary<Widget, DockPosition> _dockPositions = new();

    #endregion

    #region Public Methods

    // 将子组件停靠到指定位置
    public void DockWidget(Widget child, DockPosition position)
    {
        AddChild(child);
        _dockPositions[child] = position;
    }

    #endregion

    #region Layout Methods

    protected override Size MeasureChildren(Size availableSize)
    {
        float consumedTop = 0;
        float consumedBottom = 0;
        float consumedLeft = 0;
        float consumedRight = 0;

        // 第一阶段：测量 Top 和 Bottom 停靠的子组件
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (!_dockPositions.TryGetValue(child, out var pos))
            {
                continue;
            }

            if (pos == DockPosition.Top || pos == DockPosition.Bottom)
            {
                child.Measure(new Size(availableSize.Width, availableSize.Height));

                if (pos == DockPosition.Top)
                {
                    consumedTop += child.DesiredSize.Height;
                }
                else
                {
                    consumedBottom += child.DesiredSize.Height;
                }
            }
        }

        // 计算 Top/Bottom 消耗后剩余的高度
        float remainingHeight = Math.Max(0, availableSize.Height - consumedTop - consumedBottom);

        // 第二阶段：测量 Left 和 Right 停靠的子组件
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (!_dockPositions.TryGetValue(child, out var pos))
            {
                continue;
            }

            if (pos == DockPosition.Left || pos == DockPosition.Right)
            {
                child.Measure(new Size(availableSize.Width, remainingHeight));

                if (pos == DockPosition.Left)
                {
                    consumedLeft += child.DesiredSize.Width;
                }
                else
                {
                    consumedRight += child.DesiredSize.Width;
                }
            }
        }

        // 计算 Left/Right 消耗后剩余的宽度
        float remainingWidth = Math.Max(0, availableSize.Width - consumedLeft - consumedRight);

        // 第三阶段：测量 Fill 停靠的子组件，使用剩余空间
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (!_dockPositions.TryGetValue(child, out var pos))
            {
                continue;
            }

            if (pos == DockPosition.Fill)
            {
                child.Measure(new Size(remainingWidth, remainingHeight));
            }
        }

        return availableSize;
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        float top = contentRect.Y;
        float bottom = contentRect.Bottom;
        float left = contentRect.X;
        float right = contentRect.Right;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (!_dockPositions.TryGetValue(child, out var pos))
            {
                continue;
            }

            switch (pos)
            {
                case DockPosition.Top:
                    child.Arrange(new Rect(left, top, right - left, child.DesiredSize.Height));
                    top += child.DesiredSize.Height;
                    break;

                case DockPosition.Bottom:
                    child.Arrange(new Rect(left, bottom - child.DesiredSize.Height, right - left, child.DesiredSize.Height));
                    bottom -= child.DesiredSize.Height;
                    break;

                case DockPosition.Left:
                    child.Arrange(new Rect(left, top, child.DesiredSize.Width, bottom - top));
                    left += child.DesiredSize.Width;
                    break;

                case DockPosition.Right:
                    child.Arrange(new Rect(right - child.DesiredSize.Width, top, child.DesiredSize.Width, bottom - top));
                    right -= child.DesiredSize.Width;
                    break;

                case DockPosition.Fill:
                    child.Arrange(new Rect(left, top, right - left, bottom - top));
                    break;
            }
        }
    }

    #endregion
}

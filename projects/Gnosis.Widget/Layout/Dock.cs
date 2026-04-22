using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Layout;

public sealed class Dock : ContainerElement
{
    #region 字段

    private readonly Dictionary<WidgetElement, DockPosition> _dockPositions = new();

    #endregion

    #region 公共方法

    public void DockWidget(WidgetElement child, DockPosition position)
    {
        AddChild(child);
        _dockPositions[child] = position;
    }

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        float consumedTop = 0;
        float consumedBottom = 0;
        float consumedLeft = 0;
        float consumedRight = 0;

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

            if (pos is DockPosition.Top or DockPosition.Bottom)
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

        var remainingHeight = Math.Max(0, availableSize.Height - consumedTop - consumedBottom);

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

            if (pos is DockPosition.Left or DockPosition.Right)
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

        var remainingWidth = Math.Max(0, availableSize.Width - consumedLeft - consumedRight);

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
        var top = contentRect.Y;
        var bottom = contentRect.Bottom;
        var left = contentRect.X;
        var right = contentRect.Right;

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

using GnosisEngine.UI.Layout;

namespace GnosisEngine.UI;

public enum DockPosition
{
    Top,
    Bottom,
    Left,
    Right,
    Fill
}

public sealed class DockItem
{
    public Widget Widget { get; }
    public DockPosition Position { get; }

    public DockItem(Widget widget, DockPosition position)
    {
        Widget = widget;
        Position = position;
    }
}

public sealed class Dock : ContainerWidget
{
    private readonly List<DockItem> _dockItems = new();

    public IReadOnlyList<DockItem> DockItems => _dockItems;

    public void DockWidget(Widget widget, DockPosition position)
    {
        _dockItems.Add(new DockItem(widget, position));
        AddChild(widget);
    }

    protected override Size MeasureChildren(Size availableSize)
    {
        float consumedTopHeight = 0;
        float consumedBottomHeight = 0;
        float consumedLeftWidth = 0;
        float consumedRightWidth = 0;
        float maxHorizontalWidth = 0;

        // 先测量 Top/Bottom
        foreach (var item in _dockItems)
        {
            if (item.Widget.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            switch (item.Position)
            {
                case DockPosition.Top:
                case DockPosition.Bottom:
                {
                    item.Widget.Measure(availableSize);
                    if (item.Position == DockPosition.Top)
                    {
                        consumedTopHeight += item.Widget.DesiredSize.Height;
                    }
                    else
                    {
                        consumedBottomHeight += item.Widget.DesiredSize.Height;
                    }
                    maxHorizontalWidth = Math.Max(maxHorizontalWidth, item.Widget.DesiredSize.Width);
                    break;
                }
            }
        }

        // 计算剩余高度
        float remainingHeight = Math.Max(0, availableSize.Height - consumedTopHeight - consumedBottomHeight);
        var middleAvailable = new Size(availableSize.Width, remainingHeight);

        // 测量 Left/Right
        foreach (var item in _dockItems)
        {
            if (item.Widget.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            switch (item.Position)
            {
                case DockPosition.Left:
                case DockPosition.Right:
                {
                    item.Widget.Measure(middleAvailable);
                    if (item.Position == DockPosition.Left)
                    {
                        consumedLeftWidth += item.Widget.DesiredSize.Width;
                    }
                    else
                    {
                        consumedRightWidth += item.Widget.DesiredSize.Width;
                    }
                    break;
                }
            }
        }

        // 计算剩余宽度
        float remainingWidth = Math.Max(0, availableSize.Width - consumedLeftWidth - consumedRightWidth);
        var fillAvailable = new Size(remainingWidth, remainingHeight);

        // 测量 Fill
        float fillWidth = 0;
        float fillHeight = 0;
        foreach (var item in _dockItems)
        {
            if (item.Widget.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (item.Position == DockPosition.Fill)
            {
                item.Widget.Measure(fillAvailable);
                fillWidth = Math.Max(fillWidth, item.Widget.DesiredSize.Width);
                fillHeight = Math.Max(fillHeight, item.Widget.DesiredSize.Height);
            }
        }

        float totalWidth = Math.Max(maxHorizontalWidth, consumedLeftWidth + fillWidth + consumedRightWidth);
        float totalHeight = consumedTopHeight + Math.Max(remainingHeight, fillHeight) + consumedBottomHeight;

        return new Size(totalWidth, totalHeight);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        float x = contentRect.X;
        float y = contentRect.Y;
        float remainingWidth = contentRect.Width;
        float remainingHeight = contentRect.Height;

        Widget? fillWidget = null;

        foreach (var item in _dockItems)
        {
            if (item.Widget.Visibility == Visibility.Collapsed)
                continue;

            switch (item.Position)
            {
                case DockPosition.Top:
                {
                    var rect = new Rect(x, y, remainingWidth, item.Widget.DesiredSize.Height);
                    item.Widget.Arrange(rect);
                    y += item.Widget.DesiredSize.Height;
                    remainingHeight -= item.Widget.DesiredSize.Height;
                    break;
                }
                case DockPosition.Bottom:
                {
                    var rect = new Rect(x, y + remainingHeight - item.Widget.DesiredSize.Height, remainingWidth, item.Widget.DesiredSize.Height);
                    item.Widget.Arrange(rect);
                    remainingHeight -= item.Widget.DesiredSize.Height;
                    break;
                }
                case DockPosition.Left:
                {
                    var rect = new Rect(x, y, item.Widget.DesiredSize.Width, remainingHeight);
                    item.Widget.Arrange(rect);
                    x += item.Widget.DesiredSize.Width;
                    remainingWidth -= item.Widget.DesiredSize.Width;
                    break;
                }
                case DockPosition.Right:
                {
                    var rect = new Rect(x + remainingWidth - item.Widget.DesiredSize.Width, y, item.Widget.DesiredSize.Width, remainingHeight);
                    item.Widget.Arrange(rect);
                    remainingWidth -= item.Widget.DesiredSize.Width;
                    break;
                }
                case DockPosition.Fill:
                    fillWidget = item.Widget;
                    break;
            }
        }

        if (fillWidget != null)
        {
            var fillRect = new Rect(x, y, remainingWidth, remainingHeight);
            fillWidget.Arrange(fillRect);
        }
    }
}

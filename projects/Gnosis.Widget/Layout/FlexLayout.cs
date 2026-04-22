using Gnosis.Widget.Element;

namespace Gnosis.Widget.Layout;

public class FlexLayout : ContainerElement
{
    #region 属性

    public FlexDirection Direction { get; set; } = FlexDirection.Row;

    public MainAxisAlignment MainAxisAlignment { get; set; } = MainAxisAlignment.Start;

    public CrossAxisAlignment CrossAxisAlignment { get; set; } = CrossAxisAlignment.Start;

    public float Spacing { get; set; } = 0;

    public FlexWrap Wrap { get; set; } = FlexWrap.NoWrap;

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        var isHorizontal = Direction is FlexDirection.Row or FlexDirection.RowReverse;

        return isHorizontal ? MeasureHorizontal(availableSize) : MeasureVertical(availableSize);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        var isHorizontal = Direction is FlexDirection.Row or FlexDirection.RowReverse;

        if (isHorizontal)
        {
            ArrangeHorizontal(contentRect);
        }
        else
        {
            ArrangeVertical(contentRect);
        }
    }

    #endregion

    #region 水平布局

    private Size MeasureHorizontal(Size availableSize)
    {
        float totalWidth = 0;
        float maxHeight = 0;
        float totalFlex = 0;
        var visibleChildren = GetVisibleChildren();
        var first = true;

        foreach (var child in visibleChildren)
        {
            if (child is Flexible flexible)
            {
                totalFlex += flexible.Flex;
                continue;
            }

            MeasureChild(child, availableSize);

            if (!first)
            {
                totalWidth += Spacing;
            }
            first = false;

            totalWidth += child.DesiredSize.Width;
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        if (totalFlex > 0)
        {
            var remainingWidth = Math.Max(0, availableSize.Width - totalWidth);
            var perFlex = remainingWidth / totalFlex;

            foreach (var child in visibleChildren)
            {
                if (child is not Flexible)
                {
                    continue;
                }

                var flexWidth = perFlex * ((Flexible)child).Flex;
                child.Measure(new Size(flexWidth, availableSize.Height));

                if (!first)
                {
                    totalWidth += Spacing;
                }
                first = false;

                totalWidth += child.DesiredSize.Width;
                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            }
        }

        return new Size(totalWidth, maxHeight);
    }

    private void ArrangeHorizontal(Rect contentRect)
    {
        var visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
        {
            return;
        }

        var isReverse = Direction is FlexDirection.RowReverse;
        var totalMainSize = 0f;
        var first = true;

        foreach (var child in visibleChildren)
        {
            if (!first)
            {
                totalMainSize += Spacing;
            }
            first = false;
            totalMainSize += child.DesiredSize.Width;
        }

        var gaps = visibleChildren.Count > 1 ? visibleChildren.Count - 1 : 0;
        var actualSpacing = ComputeMainAxisSpacing(contentRect.Width, totalMainSize, gaps);

        var x = contentRect.X;

        if (isReverse)
        {
            for (var i = visibleChildren.Count - 1; i >= 0; i--)
            {
                var child = visibleChildren[i];
                ArrangeChildHorizontal(child, ref x, contentRect, actualSpacing);
            }
        }
        else
        {
            foreach (var child in visibleChildren)
            {
                ArrangeChildHorizontal(child, ref x, contentRect, actualSpacing);
            }
        }
    }

    private void ArrangeChildHorizontal(WidgetElement child, ref float x, Rect contentRect, float spacing)
    {
        float childY;
        float childHeight;

        switch (CrossAxisAlignment)
        {
            case CrossAxisAlignment.Center:
                childY = contentRect.Y + (contentRect.Height - child.DesiredSize.Height) / 2;
                childHeight = child.DesiredSize.Height;
                break;
            case CrossAxisAlignment.End:
                childY = contentRect.Bottom - child.DesiredSize.Height;
                childHeight = child.DesiredSize.Height;
                break;
            case CrossAxisAlignment.Stretch:
                childY = contentRect.Y;
                childHeight = contentRect.Height;
                break;
            default:
                childY = contentRect.Y;
                childHeight = child.DesiredSize.Height;
                break;
        }

        child.Arrange(new Rect(x, childY, child.DesiredSize.Width, childHeight));
        x += child.DesiredSize.Width + spacing;
    }

    #endregion

    #region 垂直布局

    private Size MeasureVertical(Size availableSize)
    {
        float totalHeight = 0;
        float maxWidth = 0;
        float totalFlex = 0;
        var visibleChildren = GetVisibleChildren();
        var first = true;

        foreach (var child in visibleChildren)
        {
            if (child is Flexible flexible)
            {
                totalFlex += flexible.Flex;
                continue;
            }

            MeasureChild(child, availableSize);

            if (!first)
            {
                totalHeight += Spacing;
            }
            first = false;

            totalHeight += child.DesiredSize.Height;
            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
        }

        if (totalFlex > 0)
        {
            var remainingHeight = Math.Max(0, availableSize.Height - totalHeight);
            var perFlex = remainingHeight / totalFlex;

            foreach (var child in visibleChildren)
            {
                if (child is not Flexible)
                {
                    continue;
                }

                var flexHeight = perFlex * ((Flexible)child).Flex;
                child.Measure(new Size(availableSize.Width, flexHeight));

                if (!first)
                {
                    totalHeight += Spacing;
                }
                first = false;

                totalHeight += child.DesiredSize.Height;
                maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
            }
        }

        return new Size(maxWidth, totalHeight);
    }

    private void ArrangeVertical(Rect contentRect)
    {
        var visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
        {
            return;
        }

        var isReverse = Direction is FlexDirection.ColumnReverse;
        var totalMainSize = 0f;
        var first = true;

        foreach (var child in visibleChildren)
        {
            if (!first)
            {
                totalMainSize += Spacing;
            }
            first = false;
            totalMainSize += child.DesiredSize.Height;
        }

        var gaps = visibleChildren.Count > 1 ? visibleChildren.Count - 1 : 0;
        var actualSpacing = ComputeMainAxisSpacing(contentRect.Height, totalMainSize, gaps);

        var y = contentRect.Y;

        if (isReverse)
        {
            for (var i = visibleChildren.Count - 1; i >= 0; i--)
            {
                var child = visibleChildren[i];
                ArrangeChildVertical(child, ref y, contentRect, actualSpacing);
            }
        }
        else
        {
            foreach (var child in visibleChildren)
            {
                ArrangeChildVertical(child, ref y, contentRect, actualSpacing);
            }
        }
    }

    private void ArrangeChildVertical(WidgetElement child, ref float y, Rect contentRect, float spacing)
    {
        float childX;
        float childWidth;

        switch (CrossAxisAlignment)
        {
            case CrossAxisAlignment.Center:
                childX = contentRect.X + (contentRect.Width - child.DesiredSize.Width) / 2;
                childWidth = child.DesiredSize.Width;
                break;
            case CrossAxisAlignment.End:
                childX = contentRect.Right - child.DesiredSize.Width;
                childWidth = child.DesiredSize.Width;
                break;
            case CrossAxisAlignment.Stretch:
                childX = contentRect.X;
                childWidth = contentRect.Width;
                break;
            default:
                childX = contentRect.X;
                childWidth = child.DesiredSize.Width;
                break;
        }

        child.Arrange(new Rect(childX, y, childWidth, child.DesiredSize.Height));
        y += child.DesiredSize.Height + spacing;
    }

    #endregion

    #region 辅助方法

    private float ComputeMainAxisSpacing(float availableLength, float contentLength, int gaps)
    {
        if (gaps <= 0)
        {
            return Spacing;
        }

        var freeSpace = availableLength - contentLength;

        return MainAxisAlignment switch
        {
            MainAxisAlignment.SpaceBetween => gaps > 0 ? freeSpace / gaps : 0,
            MainAxisAlignment.SpaceAround => gaps > 0 ? freeSpace / (gaps + 1) : 0,
            MainAxisAlignment.SpaceEvenly => freeSpace / (gaps + 1),
            _ => Spacing
        };
    }

    private List<WidgetElement> GetVisibleChildren()
    {
        var result = new List<WidgetElement>();

        foreach (var child in Children)
        {
            if (child.Visibility != Visibility.Collapsed)
            {
                result.Add(child);
            }
        }

        return result;
    }

    private void MeasureChild(WidgetElement child, Size availableSize)
    {
        child.Measure(availableSize);
    }

    #endregion
}

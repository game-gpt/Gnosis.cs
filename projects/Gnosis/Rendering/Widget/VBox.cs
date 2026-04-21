namespace Gnosis.Rendering.Widget;

public sealed class VBox : ContainerWidget
{
    public float Spacing { get; set; } = 0;

    // 交叉轴对齐方式
    public CrossAxisAlignment CrossAxisAlignment { get; set; } = CrossAxisAlignment.Start;

    protected override Size MeasureChildren(Size availableSize)
    {
        float totalHeight = 0;
        float maxWidth = 0;
        float totalFlex = 0;
        var first = true;

        // 先测量非弹性子组件
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (child is Flexible flexible)
            {
                totalFlex += flexible.Flex;
                continue;
            }

            if (CrossAxisAlignment == CrossAxisAlignment.Stretch)
            {
                child.Measure(new Size(availableSize.Width, availableSize.Height));
            }
            else
            {
                child.Measure(availableSize);
            }

            if (!first)
            {
                totalHeight += Spacing;
            }
            first = false;

            totalHeight += child.DesiredSize.Height;
            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
        }

        // 计算剩余高度并按 flex 比例分配给弹性子组件
        if (totalFlex > 0)
        {
            var remainingHeight = Math.Max(0, availableSize.Height - totalHeight - (totalFlex > 0 && Children.Count > totalFlex ? Spacing : 0) * (Children.Count(c => c is Flexible && c.Visibility != Visibility.Collapsed) - 1));
            var perFlex = remainingHeight / totalFlex;

            foreach (var child in Children)
            {
                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                if (child is Flexible flexible)
                {
                    var flexHeight = perFlex * flexible.Flex;
                    var flexAvailable = new Size(
                        CrossAxisAlignment == CrossAxisAlignment.Stretch ? availableSize.Width : availableSize.Width,
                        flexHeight
                    );
                    child.Measure(flexAvailable);

                    if (!first)
                    {
                        totalHeight += Spacing;
                    }
                    first = false;

                    totalHeight += child.DesiredSize.Height;
                    maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
                }
            }
        }

        return new Size(maxWidth, totalHeight);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        var y = contentRect.Y;
        var first = true;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (!first)
            {
                y += Spacing;
            }
            first = false;

            float childX;
            float childWidth;

            switch (CrossAxisAlignment)
            {
                case CrossAxisAlignment.Start:
                    childX = contentRect.X;
                    childWidth = child.DesiredSize.Width;
                    break;

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

            var childRect = new Rect(
                childX,
                y,
                childWidth,
                child.DesiredSize.Height
            );

            child.Arrange(childRect);
            y += child.DesiredSize.Height;
        }
    }
}

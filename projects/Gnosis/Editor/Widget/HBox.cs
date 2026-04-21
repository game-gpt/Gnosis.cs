namespace Gnosis.Editor.Widget;

public sealed class HBox : ContainerWidget
{
    public float Spacing { get; set; } = 0;

    // 交叉轴对齐方式
    public CrossAxisAlignment CrossAxisAlignment { get; set; } = CrossAxisAlignment.Start;

    protected override Size MeasureChildren(Size availableSize)
    {
        float totalWidth = 0;
        float maxHeight = 0;
        float totalFlex = 0;
        bool first = true;

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
                totalWidth += Spacing;
            }
            first = false;

            totalWidth += child.DesiredSize.Width;
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        // 计算剩余宽度并按 flex 比例分配给弹性子组件
        if (totalFlex > 0)
        {
            float remainingWidth = Math.Max(0, availableSize.Width - totalWidth);
            float perFlex = remainingWidth / totalFlex;

            foreach (var child in Children)
            {
                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                if (child is Flexible flexible)
                {
                    float flexWidth = perFlex * flexible.Flex;
                    var flexAvailable = new Size(flexWidth, availableSize.Height);
                    child.Measure(flexAvailable);

                    if (!first)
                    {
                        totalWidth += Spacing;
                    }
                    first = false;

                    totalWidth += child.DesiredSize.Width;
                    maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
                }
            }
        }

        return new Size(totalWidth, maxHeight);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        float x = contentRect.X;
        bool first = true;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (!first)
            {
                x += Spacing;
            }
            first = false;

            float childY;
            float childHeight;

            switch (CrossAxisAlignment)
            {
                case CrossAxisAlignment.Start:
                    childY = contentRect.Y;
                    childHeight = child.DesiredSize.Height;
                    break;

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

            var childRect = new Rect(
                x,
                childY,
                child.DesiredSize.Width,
                childHeight
            );

            child.Arrange(childRect);
            x += child.DesiredSize.Width;
        }
    }
}

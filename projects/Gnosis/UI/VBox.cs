using GnosisEngine.UI.Layout;

namespace GnosisEngine.UI;

public sealed class VBox : ContainerWidget
{
    public float Spacing { get; set; } = 0;

    // 交叉轴对齐方式
    public CrossAxisAlignment CrossAxisAlignment { get; set; } = CrossAxisAlignment.Start;

    protected override Size MeasureChildren(Size availableSize)
    {
        float totalHeight = 0;
        float maxWidth = 0;
        bool first = true;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            // Stretch 模式下使用可用宽度约束子组件测量
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

        return new Size(maxWidth, totalHeight);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        float y = contentRect.Y;
        bool first = true;

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

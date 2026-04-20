using GnosisEngine.UI.Layout;

namespace GnosisEngine.UI;

public sealed class ClipRect : ContainerWidget
{
    protected override Size MeasureChildren(Size availableSize)
    {
        float maxWidth = 0;
        float maxHeight = 0;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Measure(availableSize);
            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        return new Size(maxWidth, maxHeight);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Arrange(contentRect);
        }
    }

    public override void Paint(UiRenderer renderer)
    {
        var contentRect = LayoutRect.Deflate(Margin).Deflate(Padding);

        renderer.PushClip(contentRect.X, contentRect.Y, contentRect.Width, contentRect.Height);

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Paint(renderer);
        }

        renderer.PopClip();
    }
}

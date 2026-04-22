using Gnosis.Widget.Element;

namespace Gnosis.Widget.Layout;

public interface IStackPositioned
{
    float Left { get; }
    float Top { get; }
}

public sealed class Stack : ContainerElement
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

            var x = contentRect.X;
            var y = contentRect.Y;

            if (child is IStackPositioned positioned)
            {
                x += positioned.Left;
                y += positioned.Top;
            }

            child.Arrange(new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height));
        }
    }
}

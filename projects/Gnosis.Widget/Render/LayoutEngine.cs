using Gnosis.Widget.Element;

namespace Gnosis.Widget.Render;

public sealed class LayoutEngine : ILayoutEngine
{
    public void Layout(WidgetElement root, Size availableSize)
    {
        if (!IsDirty(root))
        {
            return;
        }

        root.Measure(availableSize);
        root.Arrange(new Rect(0, 0, availableSize.Width, availableSize.Height));
    }

    public void Layout(WidgetElement root, float width, float height)
    {
        Layout(root, new Size(width, height));
    }

    private static bool IsDirty(WidgetElement widget)
    {
        if (widget.IsMeasureDirty() || widget.IsArrangeDirty())
        {
            return true;
        }

        if (widget is ContainerElement container)
        {
            foreach (var child in container.Children)
            {
                if (IsDirty(child))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

using Gnosis.Widget.Element;

namespace Gnosis.Widget.Render;

public sealed class WidgetTreeRenderer : IWidgetTreeRenderer
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly UiRenderer _renderer;

    public WidgetTreeRenderer(UiRenderer renderer)
    {
        _renderer = renderer;
    }

    public void Render(WidgetElement root, float width, float height)
    {
        _layoutEngine.Layout(root, width, height);

        PaintWidget(root);
    }

    private void PaintWidget(WidgetElement widget)
    {
        widget.Paint(_renderer);

        if (widget is ContainerElement container)
        {
            foreach (var child in container.Children)
            {
                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                PaintWidget(child);
            }
        }
    }
}

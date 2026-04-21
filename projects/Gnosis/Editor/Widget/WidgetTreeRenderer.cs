namespace Gnosis.Editor.Widget;

public sealed class WidgetTreeRenderer : IWidgetTreeRenderer
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly UiRenderer _renderer;

    public WidgetTreeRenderer(UiRenderer renderer)
    {
        _renderer = renderer;
    }

    public void Render(Widget root, float width, float height)
    {
        _layoutEngine.Layout(root, width, height);

        PaintWidget(root);
    }

    void IWidgetTreeRenderer.Render(IWidget root, float width, float height)
    {
        if (root is Widget widget)
        {
            Render(widget, width, height);
        }
    }

    private void PaintWidget(Widget widget)
    {
        widget.Paint(_renderer);

        if (widget is ContainerWidget container)
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

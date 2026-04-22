using Gnosis.Widget.Render;

namespace Gnosis.Widget.Element;

public abstract class ContainerElement : WidgetElement, IContainerElement
{
    private readonly List<WidgetElement> _children = [];

    public IReadOnlyList<WidgetElement> Children => _children;

    IReadOnlyList<IWidgetElement> IContainerElement.Children => _children.Cast<IWidgetElement>().ToList().AsReadOnly();

    public void AddChild(WidgetElement child)
    {
        _children.Add(child);
        child.Parent = this;
        InvalidateMeasure();
    }

    void IContainerElement.AddChild(IWidgetElement child)
    {
        if (child is WidgetElement widget)
        {
            AddChild(widget);
        }
    }

    public void RemoveChild(WidgetElement child)
    {
        _children.Remove(child);
        child.Parent = null;
        InvalidateMeasure();
    }

    void IContainerElement.RemoveChild(IWidgetElement child)
    {
        if (child is WidgetElement widget)
        {
            RemoveChild(widget);
        }
    }

    public void ClearChildren()
    {
        foreach (var child in _children)
        {
            child.Parent = null;
        }
        _children.Clear();
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return MeasureChildren(availableSize);
    }

    protected abstract Size MeasureChildren(Size availableSize);

    protected override void ArrangeOverride(Rect contentRect)
    {
        ArrangeChildren(contentRect);
    }

    protected abstract void ArrangeChildren(Rect contentRect);

    public override void Paint(IWidgetRenderer renderer)
    {
        if (Background.A > 0)
        {
            renderer.DrawRect(
                LayoutRect.X, LayoutRect.Y,
                LayoutRect.Width, LayoutRect.Height,
                Background.R, Background.G, Background.B, Background.A
            );
        }

        if (Border.Horizontal > 0 || Border.Vertical > 0)
        {
            PaintBorder(renderer);
        }

        var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);

        PaintBackground(renderer, contentRect);

        foreach (var child in _children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Paint(renderer);
        }
    }

    protected virtual void PaintBackground(IWidgetRenderer renderer, Rect contentRect) { }

    private void PaintBorder(IWidgetRenderer renderer)
    {
        if (BorderColor.A <= 0)
        {
            return;
        }

        var borderRect = LayoutRect.Deflate(Margin);

        if (Border.Top > 0)
        {
            renderer.DrawRect(
                borderRect.X, borderRect.Y,
                borderRect.Width, Border.Top,
                BorderColor.R, BorderColor.G, BorderColor.B, BorderColor.A
            );
        }

        if (Border.Bottom > 0)
        {
            renderer.DrawRect(
                borderRect.X, borderRect.Bottom - Border.Bottom,
                borderRect.Width, Border.Bottom,
                BorderColor.R, BorderColor.G, BorderColor.B, BorderColor.A
            );
        }

        if (Border.Left > 0)
        {
            renderer.DrawRect(
                borderRect.X, borderRect.Y + Border.Top,
                Border.Left, borderRect.Height - Border.Top - Border.Bottom,
                BorderColor.R, BorderColor.G, BorderColor.B, BorderColor.A
            );
        }

        if (Border.Right > 0)
        {
            renderer.DrawRect(
                borderRect.Right - Border.Right, borderRect.Y + Border.Top,
                Border.Right, borderRect.Height - Border.Top - Border.Bottom,
                BorderColor.R, BorderColor.G, BorderColor.B, BorderColor.A
            );
        }
    }
}

using GnosisEngine.UI.Layout;

namespace GnosisEngine.UI;

public abstract class ContainerWidget : Widget
{
    private readonly List<Widget> _children = new();

    public IReadOnlyList<Widget> Children => _children;

    public void AddChild(Widget child)
    {
        _children.Add(child);
        child.Parent = this;
        InvalidateMeasure();
    }

    public void RemoveChild(Widget child)
    {
        _children.Remove(child);
        child.Parent = null;
        InvalidateMeasure();
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

    public override void Paint(UiRenderer renderer)
    {
        if (Background.A > 0)
        {
            renderer.DrawRect(
                LayoutRect.X, LayoutRect.Y,
                LayoutRect.Width, LayoutRect.Height,
                Background.R, Background.G, Background.B, Background.A
            );
        }

        var contentRect = LayoutRect.Deflate(Margin).Deflate(Padding);

        PaintBackground(renderer, contentRect);

        foreach (var child in _children)
        {
            if (child.Visibility == Visibility.Collapsed)
                continue;

            child.Paint(renderer);
        }
    }

    protected virtual void PaintBackground(UiRenderer renderer, Rect contentRect) { }
}

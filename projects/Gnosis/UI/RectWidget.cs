using GnosisEngine.UI.Layout;

namespace GnosisEngine.UI;

public sealed class RectWidget : Widget
{
    public Color Fill { get; set; } = Color.Transparent;

    public RectWidget() { }

    public RectWidget(Color fill)
    {
        Fill = fill;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(
            Width ?? MinWidth,
            Height ?? MinHeight
        );
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    public override void Paint(UiRenderer renderer)
    {
        var rect = LayoutRect.Deflate(Margin);

        if (Fill.A > 0)
        {
            renderer.DrawRect(
                rect.X, rect.Y,
                rect.Width, rect.Height,
                Fill.R, Fill.G, Fill.B, Fill.A
            );
        }
    }
}

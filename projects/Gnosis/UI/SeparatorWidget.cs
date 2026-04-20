using GnosisEngine.UI.Layout;

namespace GnosisEngine.UI;

public enum SeparatorDirection
{
    Horizontal,
    Vertical
}

public sealed class SeparatorWidget : Widget
{
    public SeparatorDirection Direction { get; set; } = SeparatorDirection.Horizontal;
    public Color LineColor { get; set; } = new Color(0.3f, 0.3f, 0.35f);
    public float Thickness { get; set; } = 1;

    protected override Size MeasureOverride(Size availableSize)
    {
        return Direction switch
        {
            SeparatorDirection.Horizontal => new Size(availableSize.Width, Thickness),
            SeparatorDirection.Vertical => new Size(Thickness, availableSize.Height),
            _ => Size.Zero
        };
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    public override void Paint(UiRenderer renderer)
    {
        var rect = LayoutRect.Deflate(Margin);

        if (LineColor.A > 0)
        {
            renderer.DrawRect(
                rect.X, rect.Y,
                rect.Width, rect.Height,
                LineColor.R, LineColor.G, LineColor.B, LineColor.A
            );
        }
    }
}

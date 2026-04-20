using GnosisEngine.UI.Layout;

namespace GnosisEngine.UI;

public sealed class ButtonWidget : Widget
{
    public string Label { get; set; }
    public float FontSize { get; set; } = 12;
    public Color HoverColor { get; set; } = new Color(0.25f, 0.25f, 0.3f);
    public Color PressColor { get; set; } = new Color(0.15f, 0.15f, 0.2f);

    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }

    public ButtonWidget(string label)
    {
        Label = label;
        Padding = new EdgeInsets(6, 12, 6, 12);
        Background = new Color(0.2f, 0.2f, 0.25f);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        float textWidth = Label.Length * FontSize * 0.6f;
        float textHeight = FontSize;

        return new Size(
            textWidth + Padding.Horizontal,
            textHeight + Padding.Vertical
        );
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    public override void Paint(UiRenderer renderer)
    {
        var rect = LayoutRect.Deflate(Margin);

        var bgColor = IsPressed ? PressColor : IsHovered ? HoverColor : Background;

        if (bgColor.A > 0)
        {
            renderer.DrawRect(
                rect.X, rect.Y,
                rect.Width, rect.Height,
                bgColor.R, bgColor.G, bgColor.B, bgColor.A
            );
        }

        var contentRect = LayoutRect.Deflate(Margin).Deflate(Padding);

        renderer.DrawText(
            Label,
            contentRect.X,
            contentRect.Y,
            FontSize,
            Foreground.R,
            Foreground.G,
            Foreground.B
        );
    }
}

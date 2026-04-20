using GnosisEngine.UI.Layout;

namespace GnosisEngine.UI;

public sealed class TextWidget : Widget
{
    public string Text { get; set; }
    public float FontSize { get; set; } = 12;

    public TextWidget(string text)
    {
        Text = text;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        float textWidth = Text.Length * FontSize * 0.6f;
        float textHeight = FontSize;

        return new Size(textWidth, textHeight);
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    public override void Paint(UiRenderer renderer)
    {
        if (string.IsNullOrEmpty(Text))
            return;

        var contentRect = LayoutRect.Deflate(Margin).Deflate(Padding);

        renderer.DrawText(
            Text,
            contentRect.X,
            contentRect.Y,
            FontSize,
            Foreground.R,
            Foreground.G,
            Foreground.B
        );
    }
}

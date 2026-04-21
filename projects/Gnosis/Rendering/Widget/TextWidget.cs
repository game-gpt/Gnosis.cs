namespace Gnosis.Rendering.Widget;

// 文本叶子组件，用于显示文本内容
public sealed class TextWidget : Widget
{
    #region Properties

    // 显示的文本内容
    public string Text { get; set; }

    // 字体大小
    public float FontSize { get; set; } = 14;

    #endregion

    #region Constructors

    public TextWidget(string text)
    {
        Text = text;
    }

    #endregion

    #region Layout Methods

    protected override Size MeasureOverride(Size availableSize)
    {
        // 基于字符数和字体大小估算文本尺寸
        var estimatedWidth = Text.Length * FontSize * 0.6f;
        var height = FontSize * 1.2f;

        return new Size(
            Math.Min(estimatedWidth, availableSize.Width),
            Math.Min(height, availableSize.Height)
        );
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    #endregion

    #region Rendering

    public override void Paint(UiRenderer renderer)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        renderer.DrawText(
            Text,
            LayoutRect.X,
            LayoutRect.Y,
            FontSize,
            Foreground.R, Foreground.G, Foreground.B
        );
    }

    #endregion
}

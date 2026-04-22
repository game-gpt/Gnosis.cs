using Gnosis.Widget.Element;

namespace Gnosis.Widget.Render;

public sealed class TextWidget : WidgetElement
{
    #region 属性

    public string Text { get; set; }

    public float FontSize { get; set; } = 14;

    #endregion

    #region 构造函数

    public TextWidget(string text)
    {
        Text = text;
    }

    #endregion

    #region 布局方法

    protected override Size MeasureOverride(Size availableSize)
    {
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

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
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

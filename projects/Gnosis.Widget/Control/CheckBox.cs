using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Control;

public sealed class CheckBox : WidgetElement
{
    #region 属性

    public bool IsChecked { get; set; }

    public string Label { get; set; } = "";

    public float BoxSize { get; set; } = 14;

    public float LabelSpacing { get; set; } = 6;

    public float FontSize { get; set; } = 12;

    public Color CheckColor { get; set; } = new(0.35f, 0.55f, 0.90f, 1.0f);

    public Color UncheckedColor { get; set; } = new(0.40f, 0.40f, 0.44f, 1.0f);

    public Color HoverColor { get; set; } = new(0.50f, 0.50f, 0.54f, 1.0f);

    public bool IsHovered { get; set; }

    #endregion

    #region 布局方法

    protected override Size MeasureOverride(Size availableSize)
    {
        var labelWidth = string.IsNullOrEmpty(Label) ? 0 : Label.Length * FontSize * 0.6f;
        var totalWidth = BoxSize + LabelSpacing + labelWidth;
        var totalHeight = Math.Max(BoxSize, FontSize * 1.2f);

        return new Size(
            Math.Min(totalWidth, availableSize.Width),
            Math.Min(totalHeight, availableSize.Height)
        );
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        var x = LayoutRect.X;
        var y = LayoutRect.Y;
        var yOffset = (DesiredSize.Height - BoxSize) / 2;

        var boxColor = IsHovered ? HoverColor : UncheckedColor;

        renderer.DrawRect(
            x, y + yOffset,
            BoxSize, BoxSize,
            boxColor.R, boxColor.G, boxColor.B, boxColor.A
        );

        if (IsChecked)
        {
            renderer.DrawRect(
                x + 2, y + yOffset + 2,
                BoxSize - 4, BoxSize - 4,
                CheckColor.R, CheckColor.G, CheckColor.B, CheckColor.A
            );
        }

        if (!string.IsNullOrEmpty(Label))
        {
            renderer.DrawText(
                Label,
                x + BoxSize + LabelSpacing,
                y + (DesiredSize.Height - FontSize) / 2,
                FontSize,
                Foreground.R, Foreground.G, Foreground.B
            );
        }
    }

    #endregion
}

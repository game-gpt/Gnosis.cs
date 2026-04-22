using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Control;

public sealed class Slider : WidgetElement
{
    #region 属性

    private float _value;

    public float Value
    {
        get => _value;
        set => _value = Math.Clamp(value, Minimum, Maximum);
    }

    public float Minimum { get; set; } = 0;

    public float Maximum { get; set; } = 100;

    public float TrackHeight { get; set; } = 4;

    public float ThumbSize { get; set; } = 12;

    public float SliderHeight { get; set; } = 20;

    public Color TrackColor { get; set; } = new(0.30f, 0.30f, 0.34f, 1.0f);

    public Color FillColor { get; set; } = new(0.35f, 0.55f, 0.90f, 1.0f);

    public Color ThumbColor { get; set; } = new(0.90f, 0.90f, 0.92f, 1.0f);

    public Color ThumbHoverColor { get; set; } = new(1.0f, 1.0f, 1.0f, 1.0f);

    public bool IsHovered { get; set; }

    public bool IsDragging { get; set; }

    #endregion

    #region 布局方法

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(
            Math.Min(availableSize.Width, 200),
            Math.Min(SliderHeight, availableSize.Height)
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
        var width = LayoutRect.Width;
        var centerY = y + SliderHeight / 2;

        renderer.DrawRect(
            x, centerY - TrackHeight / 2,
            width, TrackHeight,
            TrackColor.R, TrackColor.G, TrackColor.B, TrackColor.A
        );

        var ratio = Maximum > Minimum ? (Value - Minimum) / (Maximum - Minimum) : 0;
        var fillWidth = width * ratio;

        renderer.DrawRect(
            x, centerY - TrackHeight / 2,
            fillWidth, TrackHeight,
            FillColor.R, FillColor.G, FillColor.B, FillColor.A
        );

        var thumbX = x + fillWidth - ThumbSize / 2;
        var thumbColor = IsHovered || IsDragging ? ThumbHoverColor : ThumbColor;

        renderer.DrawRect(
            thumbX, centerY - ThumbSize / 2,
            ThumbSize, ThumbSize,
            thumbColor.R, thumbColor.G, thumbColor.B, thumbColor.A
        );
    }

    #endregion

    #region 公开方法

    public void SetValueFromPosition(float localX)
    {
        var width = LayoutRect.Width;
        if (width <= 0)
        {
            return;
        }

        var ratio = Math.Clamp(localX / width, 0, 1);
        Value = Minimum + ratio * (Maximum - Minimum);
    }

    #endregion
}

using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Scene;

public enum ViewportMode
{
    Perspective,
    Top,
    Bottom,
    Left,
    Right,
    Front,
    Back
}

public sealed class Viewport3D : WidgetElement
{
    #region 属性

    public ViewportMode Mode { get; set; } = ViewportMode.Perspective;

    public float FieldOfView { get; set; } = 60.0f;

    public float NearClip { get; set; } = 0.1f;

    public float FarClip { get; set; } = 1000.0f;

    public Color GridColor { get; set; } = new(0.25f, 0.25f, 0.28f, 0.50f);

    public Color AxisXColor { get; set; } = new(0.85f, 0.25f, 0.25f, 1.0f);

    public Color AxisYColor { get; set; } = new(0.25f, 0.85f, 0.25f, 1.0f);

    public Color AxisZColor { get; set; } = new(0.25f, 0.25f, 0.85f, 1.0f);

    public float GridSize { get; set; } = 10.0f;

    public float GridStep { get; set; } = 1.0f;

    public bool ShowGrid { get; set; } = true;

    public bool ShowAxes { get; set; } = true;

    public new bool IsFocused { get; set; }

    #endregion

    #region 布局方法

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(
            Width ?? availableSize.Width,
            Height ?? availableSize.Height
        );
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        var rect = LayoutRect.Deflate(Margin);

        renderer.DrawRect(
            rect.X, rect.Y,
            rect.Width, rect.Height,
            Background.R, Background.G, Background.B, Background.A
        );

        if (ShowGrid)
        {
            PaintGrid(renderer, rect);
        }

        if (ShowAxes)
        {
            PaintAxes(renderer, rect);
        }

        PaintOverlay(renderer, rect);
    }

    private void PaintGrid(IWidgetRenderer renderer, Rect rect)
    {
        var centerX = rect.X + rect.Width / 2;
        var centerY = rect.Y + rect.Height / 2;

        var halfGrid = GridSize / 2;

        for (var i = -halfGrid; i <= halfGrid; i += GridStep)
        {
            var offset = i * 20;

            renderer.DrawLine(
                centerX + offset, rect.Y,
                centerX + offset, rect.Bottom,
                GridColor.R, GridColor.G, GridColor.B
            );

            renderer.DrawLine(
                rect.X, centerY + offset,
                rect.Right, centerY + offset,
                GridColor.R, GridColor.G, GridColor.B
            );
        }
    }

    private void PaintAxes(IWidgetRenderer renderer, Rect rect)
    {
        var originX = rect.X + 30;
        var originY = rect.Bottom - 30;
        var axisLength = 20;

        renderer.DrawLine(
            originX, originY,
            originX + axisLength, originY,
            AxisXColor.R, AxisXColor.G, AxisXColor.B
        );

        renderer.DrawLine(
            originX, originY,
            originX, originY - axisLength,
            AxisYColor.R, AxisYColor.G, AxisYColor.B
        );

        renderer.DrawText("X", originX + axisLength + 2, originY - 4, 8, AxisXColor.R, AxisXColor.G, AxisXColor.B);
        renderer.DrawText("Y", originX - 4, originY - axisLength - 10, 8, AxisYColor.R, AxisYColor.G, AxisYColor.B);
    }

    private void PaintOverlay(IWidgetRenderer renderer, Rect rect)
    {
        var modeText = Mode.ToString();
        renderer.DrawText(modeText, rect.X + 8, rect.Y + 6, 10, 0.5f, 0.5f, 0.54f);
    }

    #endregion
}

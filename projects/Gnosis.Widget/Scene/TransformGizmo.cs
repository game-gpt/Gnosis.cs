using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Scene;

public enum TransformMode
{
    Translate,
    Rotate,
    Scale
}

public enum TransformSpace
{
    World,
    Local
}

public sealed class TransformGizmo : WidgetElement
{
    #region 属性

    public TransformMode Mode { get; set; } = TransformMode.Translate;

    public TransformSpace Space { get; set; } = TransformSpace.World;

    public float GizmoSize { get; set; } = 60;

    public Color AxisXColor { get; set; } = new(0.85f, 0.25f, 0.25f, 1.0f);

    public Color AxisYColor { get; set; } = new(0.25f, 0.85f, 0.25f, 1.0f);

    public Color AxisZColor { get; set; } = new(0.25f, 0.25f, 0.85f, 1.0f);

    public Color ActiveAxisColor { get; set; } = new(1.0f, 1.0f, 0.30f, 1.0f);

    public int ActiveAxis { get; set; }

    public bool IsVisible { get; set; } = true;

    #endregion

    #region 布局方法

    protected override Size MeasureOverride(Size availableSize)
    {
        return Size.Zero;
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        if (!IsVisible)
        {
            return;
        }

        var centerX = LayoutRect.X + LayoutRect.Width / 2;
        var centerY = LayoutRect.Y + LayoutRect.Height / 2;

        switch (Mode)
        {
            case TransformMode.Translate:
                PaintTranslateGizmo(renderer, centerX, centerY);
                break;
            case TransformMode.Rotate:
                PaintRotateGizmo(renderer, centerX, centerY);
                break;
            case TransformMode.Scale:
                PaintScaleGizmo(renderer, centerX, centerY);
                break;
        }
    }

    private void PaintTranslateGizmo(IWidgetRenderer renderer, float cx, float cy)
    {
        var xColor = ActiveAxis == 1 ? ActiveAxisColor : AxisXColor;
        var yColor = ActiveAxis == 2 ? ActiveAxisColor : AxisYColor;
        var zColor = ActiveAxis == 3 ? ActiveAxisColor : AxisZColor;

        renderer.DrawLine(cx, cy, cx + GizmoSize, cy, xColor.R, xColor.G, xColor.B);
        renderer.DrawLine(cx, cy, cx, cy - GizmoSize, yColor.R, yColor.G, yColor.B);

        renderer.DrawRect(cx + GizmoSize - 4, cy - 4, 8, 8, xColor.R, xColor.G, xColor.B, xColor.A);
        renderer.DrawRect(cx - 4, cy - GizmoSize - 4, 8, 8, yColor.R, yColor.G, yColor.B, yColor.A);

        renderer.DrawText("X", cx + GizmoSize + 4, cy - 4, 8, xColor.R, xColor.G, xColor.B);
        renderer.DrawText("Y", cx - 4, cy - GizmoSize - 12, 8, yColor.R, yColor.G, yColor.B);
    }

    private void PaintRotateGizmo(IWidgetRenderer renderer, float cx, float cy)
    {
        var xColor = ActiveAxis == 1 ? ActiveAxisColor : AxisXColor;
        var yColor = ActiveAxis == 2 ? ActiveAxisColor : AxisYColor;

        renderer.DrawLine(cx - GizmoSize, cy, cx + GizmoSize, cy, xColor.R, xColor.G, xColor.B);
        renderer.DrawLine(cx, cy - GizmoSize, cx, cy + GizmoSize, yColor.R, yColor.G, yColor.B);

        renderer.DrawText("Rx", cx + GizmoSize + 4, cy - 4, 8, xColor.R, xColor.G, xColor.B);
        renderer.DrawText("Ry", cx - 8, cy - GizmoSize - 12, 8, yColor.R, yColor.G, yColor.B);
    }

    private void PaintScaleGizmo(IWidgetRenderer renderer, float cx, float cy)
    {
        var xColor = ActiveAxis == 1 ? ActiveAxisColor : AxisXColor;
        var yColor = ActiveAxis == 2 ? ActiveAxisColor : AxisYColor;

        renderer.DrawLine(cx, cy, cx + GizmoSize, cy, xColor.R, xColor.G, xColor.B);
        renderer.DrawLine(cx, cy, cx, cy - GizmoSize, yColor.R, yColor.G, yColor.B);

        renderer.DrawRect(cx + GizmoSize - 6, cy - 6, 12, 12, xColor.R, xColor.G, xColor.B, xColor.A);
        renderer.DrawRect(cx - 6, cy - GizmoSize - 6, 12, 12, yColor.R, yColor.G, yColor.B, yColor.A);

        renderer.DrawText("Sx", cx + GizmoSize + 4, cy - 4, 8, xColor.R, xColor.G, xColor.B);
        renderer.DrawText("Sy", cx - 8, cy - GizmoSize - 12, 8, yColor.R, yColor.G, yColor.B);
    }

    #endregion
}

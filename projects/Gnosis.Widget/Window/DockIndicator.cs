using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Window;

public sealed class DockIndicator : WidgetElement
{
    #region 属性

    public DockZone? ActiveZone { get; set; }

    public float IndicatorSize { get; set; } = 40;

    public float CenterSize { get; set; } = 30;

    public Color IndicatorColor { get; set; } = new(0.30f, 0.55f, 0.90f, 0.30f);

    public Color IndicatorBorderColor { get; set; } = new(0.30f, 0.55f, 0.90f, 0.80f);

    public Color CenterColor { get; set; } = new(0.30f, 0.55f, 0.90f, 0.25f);

    public bool IsVisible { get; set; }

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
        if (!IsVisible || ActiveZone == null)
        {
            return;
        }

        var cx = LayoutRect.X + LayoutRect.Width / 2;
        var cy = LayoutRect.Y + LayoutRect.Height / 2;
        var half = IndicatorSize / 2;
        var centerHalf = CenterSize / 2;

        PaintCenterIndicator(renderer, cx, cy, centerHalf);

        PaintZoneIndicator(renderer, cx, cy, half, DockZone.Left, -half - CenterSize, -centerHalf);
        PaintZoneIndicator(renderer, cx, cy, half, DockZone.Right, centerHalf, -centerHalf);
        PaintZoneIndicator(renderer, cx, cy, half, DockZone.Top, -centerHalf, -half - CenterSize);
        PaintZoneIndicator(renderer, cx, cy, half, DockZone.Bottom, -centerHalf, centerHalf);
    }

    private void PaintCenterIndicator(IWidgetRenderer renderer, float cx, float cy, float half)
    {
        var isActive = ActiveZone == DockZone.Center;
        var color = isActive ? IndicatorColor : CenterColor;

        renderer.DrawRect(
            cx - half, cy - half,
            half * 2, half * 2,
            color.R, color.G, color.B, color.A
        );

        var borderColor = IndicatorBorderColor;
        renderer.DrawRect(
            cx - half, cy - half,
            half * 2, 2,
            borderColor.R, borderColor.G, borderColor.B, borderColor.A
        );
        renderer.DrawRect(
            cx - half, cy + half - 2,
            half * 2, 2,
            borderColor.R, borderColor.G, borderColor.B, borderColor.A
        );
        renderer.DrawRect(
            cx - half, cy - half,
            2, half * 2,
            borderColor.R, borderColor.G, borderColor.B, borderColor.A
        );
        renderer.DrawRect(
            cx + half - 2, cy - half,
            2, half * 2,
            borderColor.R, borderColor.G, borderColor.B, borderColor.A
        );
    }

    private void PaintZoneIndicator(IWidgetRenderer renderer, float cx, float cy, float half, DockZone zone, float offsetX, float offsetY)
    {
        var isActive = ActiveZone == zone;
        var color = isActive ? IndicatorColor : CenterColor;

        var x = cx + offsetX;
        var y = cy + offsetY;

        renderer.DrawRect(
            x, y,
            half, half,
            color.R, color.G, color.B, color.A
        );

        if (isActive)
        {
            var borderColor = IndicatorBorderColor;
            renderer.DrawRect(
                x, y,
                half, 2,
                borderColor.R, borderColor.G, borderColor.B, borderColor.A
            );
            renderer.DrawRect(
                x, y + half - 2,
                half, 2,
                borderColor.R, borderColor.G, borderColor.B, borderColor.A
            );
            renderer.DrawRect(
                x, y,
                2, half,
                borderColor.R, borderColor.G, borderColor.B, borderColor.A
            );
            renderer.DrawRect(
                x + half - 2, y,
                2, half,
                borderColor.R, borderColor.G, borderColor.B, borderColor.A
            );
        }
    }

    #endregion

    #region 命中测试

    public DockZone? HitTestZone(float x, float y)
    {
        if (!IsVisible)
        {
            return null;
        }

        var cx = LayoutRect.X + LayoutRect.Width / 2;
        var cy = LayoutRect.Y + LayoutRect.Height / 2;
        var half = IndicatorSize / 2;
        var centerHalf = CenterSize / 2;

        if (x >= cx - centerHalf && x <= cx + centerHalf &&
            y >= cy - centerHalf && y <= cy + centerHalf)
        {
            return DockZone.Center;
        }

        var leftRect = new Rect(cx - half - CenterSize, cy - centerHalf, half, half);
        if (leftRect.Contains(x, y))
        {
            return DockZone.Left;
        }

        var rightRect = new Rect(cx + centerHalf, cy - centerHalf, half, half);
        if (rightRect.Contains(x, y))
        {
            return DockZone.Right;
        }

        var topRect = new Rect(cx - centerHalf, cy - half - CenterSize, half, half);
        if (topRect.Contains(x, y))
        {
            return DockZone.Top;
        }

        var bottomRect = new Rect(cx - centerHalf, cy + centerHalf, half, half);
        if (bottomRect.Contains(x, y))
        {
            return DockZone.Bottom;
        }

        return null;
    }

    public DockZone? DetectDockZone(float mouseX, float mouseY, float canvasWidth, float canvasHeight, float edgeThreshold = 20)
    {
        if (!IsVisible)
        {
            return null;
        }

        var indicatorZone = HitTestZone(mouseX, mouseY);
        if (indicatorZone != null)
        {
            return indicatorZone;
        }

        if (mouseX <= edgeThreshold)
        {
            return DockZone.Left;
        }

        if (mouseX >= canvasWidth - edgeThreshold)
        {
            return DockZone.Right;
        }

        if (mouseY <= edgeThreshold)
        {
            return DockZone.Top;
        }

        if (mouseY >= canvasHeight - edgeThreshold)
        {
            return DockZone.Bottom;
        }

        return null;
    }

    #endregion
}

using Gnosis.Widget.Element;

namespace Gnosis.Widget.Render;

public enum SeparatorOrientation
{
    Horizontal,
    Vertical
}

public sealed class SeparatorWidget : WidgetElement
{
    #region 属性

    public SeparatorOrientation Orientation { get; set; } = SeparatorOrientation.Horizontal;

    public float Thickness { get; set; } = 1;

    public Color SeparatorColor { get; set; } = new(0.3f, 0.3f, 0.3f, 1.0f);

    #endregion

    #region 布局方法

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Orientation == SeparatorOrientation.Horizontal)
        {
            return new Size(availableSize.Width, Thickness);
        }

        return new Size(Thickness, availableSize.Height);
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        if (Orientation == SeparatorOrientation.Horizontal)
        {
            renderer.DrawLine(
                LayoutRect.X, LayoutRect.Y + LayoutRect.Height * 0.5f,
                LayoutRect.X + LayoutRect.Width, LayoutRect.Y + LayoutRect.Height * 0.5f,
                SeparatorColor.R, SeparatorColor.G, SeparatorColor.B, SeparatorColor.A,
                Thickness
            );
        }
        else
        {
            renderer.DrawLine(
                LayoutRect.X + LayoutRect.Width * 0.5f, LayoutRect.Y,
                LayoutRect.X + LayoutRect.Width * 0.5f, LayoutRect.Y + LayoutRect.Height,
                SeparatorColor.R, SeparatorColor.G, SeparatorColor.B, SeparatorColor.A,
                Thickness
            );
        }
    }

    #endregion
}

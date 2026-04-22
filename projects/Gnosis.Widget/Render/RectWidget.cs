using Gnosis.Widget.Element;

namespace Gnosis.Widget.Render;

public sealed class RectWidget : WidgetElement
{
    #region 布局方法

    protected override Size MeasureOverride(Size availableSize)
    {
        return availableSize;
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        if (Background.A > 0)
        {
            renderer.DrawRect(
                LayoutRect.X, LayoutRect.Y,
                LayoutRect.Width, LayoutRect.Height,
                Background.R, Background.G, Background.B, Background.A
            );
        }
    }

    #endregion
}

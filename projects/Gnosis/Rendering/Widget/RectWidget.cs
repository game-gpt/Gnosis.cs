namespace Gnosis.Rendering.Widget;

// 矩形叶子组件，用于绘制填充矩形区域
public sealed class RectWidget : Widget
{
    #region Layout Methods

    protected override Size MeasureOverride(Size availableSize)
    {
        // 矩形组件填满所有可用空间
        return availableSize;
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    #endregion

    #region Rendering

    public override void Paint(UiRenderer renderer)
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

namespace Gnosis.Rendering.Widget;

// 分隔线方向枚举
public enum SeparatorOrientation
{
    // 水平分隔线
    Horizontal,
    // 垂直分隔线
    Vertical
}

// 分隔线组件，用于在布局中绘制水平或垂直分隔线
public sealed class SeparatorWidget : Widget
{
    #region Properties

    // 分隔线方向
    public SeparatorOrientation Orientation { get; set; } = SeparatorOrientation.Horizontal;

    // 分隔线粗细
    public float Thickness { get; set; } = 1;

    // 分隔线颜色
    public Color SeparatorColor { get; set; } = new(0.3f, 0.3f, 0.3f, 1.0f);

    #endregion

    #region Layout Methods

    protected override Size MeasureOverride(Size availableSize)
    {
        // 水平模式：宽度拉伸填满可用空间，高度为粗细值
        if (Orientation == SeparatorOrientation.Horizontal)
        {
            return new Size(availableSize.Width, Thickness);
        }

        // 垂直模式：高度拉伸填满可用空间，宽度为粗细值
        return new Size(Thickness, availableSize.Height);
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    #endregion

    #region Rendering

    public override void Paint(UiRenderer renderer)
    {
        renderer.DrawRect(
            LayoutRect.X, LayoutRect.Y,
            LayoutRect.Width, LayoutRect.Height,
            SeparatorColor.R, SeparatorColor.G, SeparatorColor.B, SeparatorColor.A
        );
    }

    #endregion
}

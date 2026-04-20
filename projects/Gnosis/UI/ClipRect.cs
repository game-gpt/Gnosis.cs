using GnosisEngine.UI.Layout;

namespace GnosisEngine.UI;

// 裁剪矩形容器，将子组件的绘制区域限制在内容矩形内
public sealed class ClipRect : ContainerWidget
{
    #region Layout Methods

    protected override Size MeasureChildren(Size availableSize)
    {
        float maxWidth = 0;
        float maxHeight = 0;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Measure(availableSize);
            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        return new Size(maxWidth, maxHeight);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Arrange(contentRect);
        }
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

        var contentRect = LayoutRect.Deflate(Margin).Deflate(Padding);

        // 推入裁剪区域，限制子组件绘制范围
        renderer.PushClip(contentRect.X, contentRect.Y, contentRect.Width, contentRect.Height);

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Paint(renderer);
        }

        // 弹出裁剪区域，恢复之前的绘制范围
        renderer.PopClip();
    }

    #endregion
}

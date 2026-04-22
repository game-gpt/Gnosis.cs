using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Layout;

public sealed class ClipRect : ContainerElement
{
    #region 布局方法

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

        var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);

        renderer.PushClip(contentRect.X, contentRect.Y, contentRect.Width, contentRect.Height);

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Paint(renderer);
        }

        renderer.PopClip();
    }

    #endregion
}

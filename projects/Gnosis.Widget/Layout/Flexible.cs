using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Layout;

public class Flexible : ContainerElement
{
    #region 属性

    public int Flex { get; set; } = 1;

    public FlexFit Fit { get; set; } = FlexFit.Loose;

    #endregion

    #region 构造函数

    public Flexible(WidgetElement child, int flex = 1, FlexFit fit = FlexFit.Loose)
    {
        Flex = flex;
        Fit = fit;
        AddChild(child);
    }

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        if (Children.Count == 0)
        {
            return Size.Zero;
        }

        Children[0].Measure(availableSize);
        return Children[0].DesiredSize;
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        if (Children.Count > 0)
        {
            Children[0].Arrange(contentRect);
        }
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        if (Children.Count == 0)
        {
            return;
        }

        if (Background.A > 0)
        {
            renderer.DrawRect(
                LayoutRect.X, LayoutRect.Y,
                LayoutRect.Width, LayoutRect.Height,
                Background.R, Background.G, Background.B, Background.A
            );
        }

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Paint(renderer);
        }
    }

    #endregion
}

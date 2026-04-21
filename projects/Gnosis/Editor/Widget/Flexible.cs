namespace Gnosis.Editor.Widget;

// 弹性适配模式
public enum FlexFit
{
    // 宽松模式：子组件可以自行决定大小
    Loose,
    // 紧凑模式：子组件必须填满可用空间
    Tight
}

// 弹性布局组件，用于在 Flex 容器中按比例分配剩余空间
public class Flexible : ContainerWidget
{
    #region Properties

    // 弹性系数，决定分配剩余空间的比例
    public int Flex { get; set; } = 1;

    // 弹性适配模式
    public FlexFit Fit { get; set; } = FlexFit.Loose;

    #endregion

    #region Constructors

    public Flexible(Widget child, int flex = 1, FlexFit fit = FlexFit.Loose)
    {
        Flex = flex;
        Fit = fit;
        AddChild(child);
    }

    #endregion

    #region Layout Methods

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

    #region Rendering

    public override void Paint(UiRenderer renderer)
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

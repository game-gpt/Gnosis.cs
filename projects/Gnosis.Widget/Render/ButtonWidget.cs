using Gnosis.Widget.Element;

namespace Gnosis.Widget.Render;

public sealed class ButtonWidget : ContainerElement
{
    #region 属性

    public bool IsHovered { get; set; }

    public bool IsPressed { get; set; }

    public Color HoverColor { get; set; } = new(0.25f, 0.25f, 0.28f, 1.0f);

    public Color PressedColor { get; set; } = new(0.15f, 0.15f, 0.18f, 1.0f);

    #endregion

    #region 构造函数

    public ButtonWidget() { }

    public ButtonWidget(WidgetElement child)
    {
        AddChild(child);
    }

    #endregion

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

    protected override void PaintBackground(IWidgetRenderer renderer, Rect contentRect)
    {
        Color bgColor;

        if (IsPressed)
        {
            bgColor = PressedColor;
        }
        else if (IsHovered)
        {
            bgColor = HoverColor;
        }
        else
        {
            bgColor = Background;
        }

        if (bgColor.A > 0)
        {
            renderer.DrawRect(
                contentRect.X, contentRect.Y,
                contentRect.Width, contentRect.Height,
                bgColor.R, bgColor.G, bgColor.B, bgColor.A
            );
        }
    }

    #endregion
}

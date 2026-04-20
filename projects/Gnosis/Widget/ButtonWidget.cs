using GnosisEngine.UI.Layout;

namespace GnosisEngine.UI;

// 按钮组件，支持悬停和按下状态的视觉反馈
public sealed class ButtonWidget : ContainerWidget
{
    #region Properties

    // 鼠标是否悬停在按钮上
    public bool IsHovered { get; set; }

    // 按钮是否被按下
    public bool IsPressed { get; set; }

    // 悬停状态背景色
    public Color HoverColor { get; set; } = new(0.25f, 0.25f, 0.28f, 1.0f);

    // 按下状态背景色
    public Color PressedColor { get; set; } = new(0.15f, 0.15f, 0.18f, 1.0f);

    #endregion

    #region Constructors

    public ButtonWidget() { }

    public ButtonWidget(Widget child)
    {
        AddChild(child);
    }

    #endregion

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

    protected override void PaintBackground(UiRenderer renderer, Rect contentRect)
    {
        // 根据交互状态选择背景色
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

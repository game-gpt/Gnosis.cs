using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Window;

public enum WindowState
{
    Floating,
    Docked,
    Minimized,
    Maximized
}

public sealed class WindowPane : ContainerElement
{
    #region 属性

    public string Title { get; set; } = "Untitled";

    public WindowState State { get; set; } = WindowState.Floating;

    public bool IsSelected { get; set; }

    public bool CanClose { get; set; } = true;

    public bool CanDock { get; set; } = true;

    public float TitleBarHeight { get; set; } = 24;

    public Color TitleBarColor { get; set; } = new(0.18f, 0.18f, 0.22f, 1.0f);

    public Color TitleBarActiveColor { get; set; } = new(0.22f, 0.22f, 0.28f, 1.0f);

    public Color TitleTextColor { get; set; } = new(0.85f, 0.85f, 0.88f, 1.0f);

    public Color CloseButtonColor { get; set; } = new(0.85f, 0.25f, 0.25f, 1.0f);

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        var contentAvailable = new Size(
            availableSize.Width,
            Math.Max(0, availableSize.Height - TitleBarHeight)
        );

        float maxWidth = 0;
        float maxHeight = TitleBarHeight;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Measure(contentAvailable);
            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
            maxHeight += child.DesiredSize.Height;
        }

        return new Size(maxWidth, maxHeight);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        var contentArea = new Rect(
            contentRect.X,
            contentRect.Y + TitleBarHeight,
            contentRect.Width,
            Math.Max(0, contentRect.Height - TitleBarHeight)
        );

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Arrange(contentArea);
        }
    }

    #endregion

    #region 渲染

    protected override void PaintBackground(IWidgetRenderer renderer, Rect contentRect)
    {
        var titleColor = IsSelected ? TitleBarActiveColor : TitleBarColor;

        renderer.DrawRect(
            contentRect.X, contentRect.Y,
            contentRect.Width, TitleBarHeight,
            titleColor.R, titleColor.G, titleColor.B, titleColor.A
        );

        renderer.DrawText(
            Title,
            contentRect.X + 8,
            contentRect.Y + 4,
            12,
            TitleTextColor.R, TitleTextColor.G, TitleTextColor.B
        );

        if (CanClose)
        {
            var closeX = contentRect.Right - 20;
            var closeY = contentRect.Y + 4;

            renderer.DrawRect(
                closeX, closeY,
                16, 16,
                CloseButtonColor.R, CloseButtonColor.G, CloseButtonColor.B, CloseButtonColor.A
            );
        }
    }

    #endregion
}

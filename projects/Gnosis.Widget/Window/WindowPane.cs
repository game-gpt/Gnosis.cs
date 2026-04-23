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

    public bool CanResize { get; set; } = true;

    public float TitleBarHeight { get; set; } = 24;

    public float X { get; set; }

    public float Y { get; set; }

    public Color TitleBarColor { get; set; } = new(0.18f, 0.18f, 0.22f, 1.0f);

    public Color TitleBarActiveColor { get; set; } = new(0.22f, 0.22f, 0.28f, 1.0f);

    public Color TitleTextColor { get; set; } = new(0.85f, 0.85f, 0.88f, 1.0f);

    public Color CloseButtonColor { get; set; } = new(0.85f, 0.25f, 0.25f, 1.0f);

    public Color MinimizeButtonColor { get; set; } = new(0.85f, 0.70f, 0.25f, 1.0f);

    public Color MaximizeButtonColor { get; set; } = new(0.30f, 0.75f, 0.40f, 1.0f);

    #endregion

    #region 拖拽状态

    internal bool IsDragging { get; set; }

    internal float DragOffsetX { get; set; }

    internal float DragOffsetY { get; set; }

    internal bool IsResizing { get; set; }

    internal ResizeEdge ResizeEdge { get; set; }

    internal float ResizeStartWidth { get; set; }

    internal float ResizeStartHeight { get; set; }

    internal float ResizeStartX { get; set; }

    internal float ResizeStartY { get; set; }

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

        var btnY = contentRect.Y + 4;
        var btnSize = 16;

        if (CanClose)
        {
            var closeX = contentRect.Right - 20;
            renderer.DrawRect(
                closeX, btnY,
                btnSize, btnSize,
                CloseButtonColor.R, CloseButtonColor.G, CloseButtonColor.B, CloseButtonColor.A
            );
        }

        if (State == WindowState.Floating || State == WindowState.Maximized)
        {
            var maxBtnX = contentRect.Right - (CanClose ? 40 : 20);
            renderer.DrawRect(
                maxBtnX, btnY,
                btnSize, btnSize,
                MaximizeButtonColor.R, MaximizeButtonColor.G, MaximizeButtonColor.B, MaximizeButtonColor.A
            );
        }

        var minBtnX = contentRect.Right - (CanClose ? 60 : 40);
        if (State != WindowState.Minimized)
        {
            renderer.DrawRect(
                minBtnX, btnY,
                btnSize, btnSize,
                MinimizeButtonColor.R, MinimizeButtonColor.G, MinimizeButtonColor.B, MinimizeButtonColor.A
            );
        }
    }

    #endregion

    #region 命中测试

    public bool HitTestTitleBar(float x, float y)
    {
        var contentRect = LayoutRect.Deflate(Margin);
        return x >= contentRect.X && x <= contentRect.Right &&
               y >= contentRect.Y && y <= contentRect.Y + TitleBarHeight;
    }

    public bool HitTestCloseButton(float x, float y)
    {
        if (!CanClose)
        {
            return false;
        }

        var contentRect = LayoutRect.Deflate(Margin);
        var closeX = contentRect.Right - 20;
        var closeY = contentRect.Y + 4;

        return x >= closeX && x <= closeX + 16 &&
               y >= closeY && y <= closeY + 16;
    }

    public bool HitTestMaximizeButton(float x, float y)
    {
        var contentRect = LayoutRect.Deflate(Margin);
        var maxBtnX = contentRect.Right - (CanClose ? 40 : 20);
        var btnY = contentRect.Y + 4;

        return x >= maxBtnX && x <= maxBtnX + 16 &&
               y >= btnY && y <= btnY + 16;
    }

    public bool HitTestMinimizeButton(float x, float y)
    {
        var contentRect = LayoutRect.Deflate(Margin);
        var minBtnX = contentRect.Right - (CanClose ? 60 : 40);
        var btnY = contentRect.Y + 4;

        return x >= minBtnX && x <= minBtnX + 16 &&
               y >= btnY && y <= btnY + 16;
    }

    public ResizeEdge HitTestResizeEdge(float x, float y, float threshold = 6)
    {
        if (State != WindowState.Floating || !CanResize)
        {
            return ResizeEdge.None;
        }

        var contentRect = LayoutRect.Deflate(Margin);
        var edge = ResizeEdge.None;

        if (y >= contentRect.Y && y <= contentRect.Y + threshold)
        {
            edge |= ResizeEdge.Top;
        }

        if (y >= contentRect.Bottom - threshold && y <= contentRect.Bottom)
        {
            edge |= ResizeEdge.Bottom;
        }

        if (x >= contentRect.X && x <= contentRect.X + threshold)
        {
            edge |= ResizeEdge.Left;
        }

        if (x >= contentRect.Right - threshold && x <= contentRect.Right)
        {
            edge |= ResizeEdge.Right;
        }

        return edge;
    }

    #endregion
}

[Flags]
public enum ResizeEdge
{
    None = 0,
    Top = 1,
    Bottom = 2,
    Left = 4,
    Right = 8
}

using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Control;

public sealed class ScrollView : ContainerElement
{
    #region 属性

    private float _scrollX;
    private float _scrollY;

    public float ScrollX
    {
        get => _scrollX;
        set => _scrollX = Math.Max(0, value);
    }

    public float ScrollY
    {
        get => _scrollY;
        set => _scrollY = Math.Max(0, value);
    }

    public float ScrollBarWidth { get; set; } = 10;

    public float MinThumbSize { get; set; } = 20;

    public Color ScrollBarColor { get; set; } = new(0.25f, 0.25f, 0.28f, 0.80f);

    public Color ScrollBarHoverColor { get; set; } = new(0.35f, 0.35f, 0.38f, 0.90f);

    public Color ThumbColor { get; set; } = new(0.50f, 0.50f, 0.54f, 0.80f);

    public Color ThumbHoverColor { get; set; } = new(0.60f, 0.60f, 0.64f, 0.90f);

    public bool ShowHorizontalScrollBar { get; set; }

    public bool ShowVerticalScrollBar { get; set; } = true;

    public float ContentWidth { get; private set; }

    public float ContentHeight { get; private set; }

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        var contentAvailable = new Size(
            ShowVerticalScrollBar ? availableSize.Width - ScrollBarWidth : availableSize.Width,
            ShowHorizontalScrollBar ? availableSize.Height - ScrollBarWidth : availableSize.Height
        );

        float maxW = 0;
        float maxH = 0;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Measure(new Size(float.PositiveInfinity, float.PositiveInfinity));
            maxW = Math.Max(maxW, child.DesiredSize.Width);
            maxH = Math.Max(maxH, child.DesiredSize.Height);
        }

        ContentWidth = maxW;
        ContentHeight = maxH;

        return new Size(availableSize.Width, availableSize.Height);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        var viewWidth = ShowVerticalScrollBar ? contentRect.Width - ScrollBarWidth : contentRect.Width;
        var viewHeight = ShowHorizontalScrollBar ? contentRect.Height - ScrollBarWidth : contentRect.Height;

        var clampedScrollX = Math.Max(0, Math.Min(_scrollX, Math.Max(0, ContentWidth - viewWidth)));
        var clampedScrollY = Math.Max(0, Math.Min(_scrollY, Math.Max(0, ContentHeight - viewHeight)));

        _scrollX = clampedScrollX;
        _scrollY = clampedScrollY;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Arrange(new Rect(
                contentRect.X - clampedScrollX,
                contentRect.Y - clampedScrollY,
                Math.Max(ContentWidth, viewWidth),
                Math.Max(ContentHeight, viewHeight)
            ));
        }
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
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

        PaintScrollBars(renderer, contentRect);
    }

    private void PaintScrollBars(IWidgetRenderer renderer, Rect contentRect)
    {
        var viewWidth = ShowVerticalScrollBar ? contentRect.Width - ScrollBarWidth : contentRect.Width;
        var viewHeight = ShowHorizontalScrollBar ? contentRect.Height - ScrollBarWidth : contentRect.Height;

        if (ShowVerticalScrollBar && ContentHeight > viewHeight)
        {
            var trackX = contentRect.Right - ScrollBarWidth;
            var trackY = contentRect.Y;
            var trackHeight = viewHeight;

            renderer.DrawRect(
                trackX, trackY,
                ScrollBarWidth, trackHeight,
                ScrollBarColor.R, ScrollBarColor.G, ScrollBarColor.B, ScrollBarColor.A
            );

            var thumbRatio = viewHeight / ContentHeight;
            var thumbHeight = Math.Max(MinThumbSize, trackHeight * thumbRatio);
            var thumbY = trackY + (_scrollY / Math.Max(1, ContentHeight - viewHeight)) * (trackHeight - thumbHeight);

            renderer.DrawRect(
                trackX + 1, thumbY,
                ScrollBarWidth - 2, thumbHeight,
                ThumbColor.R, ThumbColor.G, ThumbColor.B, ThumbColor.A
            );
        }

        if (ShowHorizontalScrollBar && ContentWidth > viewWidth)
        {
            var trackX = contentRect.X;
            var trackY = contentRect.Bottom - ScrollBarWidth;
            var trackWidth = viewWidth;

            renderer.DrawRect(
                trackX, trackY,
                trackWidth, ScrollBarWidth,
                ScrollBarColor.R, ScrollBarColor.G, ScrollBarColor.B, ScrollBarColor.A
            );

            var thumbRatio = viewWidth / ContentWidth;
            var thumbWidth = Math.Max(MinThumbSize, trackWidth * thumbRatio);
            var thumbX = trackX + (_scrollX / Math.Max(1, ContentWidth - viewWidth)) * (trackWidth - thumbWidth);

            renderer.DrawRect(
                thumbX, trackY + 1,
                thumbWidth, ScrollBarWidth - 2,
                ThumbColor.R, ThumbColor.G, ThumbColor.B, ThumbColor.A
            );
        }
    }

    #endregion

    #region 公开方法

    public void ScrollTo(float x, float y)
    {
        ScrollX = x;
        ScrollY = y;
        InvalidateArrange();
    }

    public void ScrollBy(float deltaX, float deltaY)
    {
        ScrollX += deltaX;
        ScrollY += deltaY;
        InvalidateArrange();
    }

    public void ScrollToTop()
    {
        ScrollY = 0;
        InvalidateArrange();
    }

    public void ScrollToBottom()
    {
        var viewHeight = ShowHorizontalScrollBar
            ? LayoutRect.Height - ScrollBarWidth
            : LayoutRect.Height;
        ScrollY = Math.Max(0, ContentHeight - viewHeight);
        InvalidateArrange();
    }

    #endregion
}

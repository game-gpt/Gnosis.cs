using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Window;

public enum SplitDirection
{
    Horizontal,
    Vertical
}

public sealed class SplitView : ContainerElement
{
    #region 属性

    public SplitDirection Direction { get; set; } = SplitDirection.Horizontal;

    public float SplitterPosition { get; set; } = 0.5f;

    public float SplitterSize { get; set; } = 4;

    public float MinFirstSize { get; set; } = 50;

    public float MinSecondSize { get; set; } = 50;

    public Color SplitterColor { get; set; } = new(0.25f, 0.25f, 0.28f, 1.0f);

    public Color SplitterHoverColor { get; set; } = new(0.35f, 0.55f, 0.90f, 1.0f);

    public bool IsSplitterHovered { get; set; }

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        if (Children.Count < 2)
        {
            return availableSize;
        }

        var first = Children[0];
        var second = Children[1];

        if (Direction == SplitDirection.Horizontal)
        {
            var firstWidth = Math.Max(MinFirstSize, availableSize.Width * SplitterPosition - SplitterSize / 2);
            var secondWidth = Math.Max(MinSecondSize, availableSize.Width * (1 - SplitterPosition) - SplitterSize / 2);

            first.Measure(new Size(firstWidth, availableSize.Height));
            second.Measure(new Size(secondWidth, availableSize.Height));
        }
        else
        {
            var firstHeight = Math.Max(MinFirstSize, availableSize.Height * SplitterPosition - SplitterSize / 2);
            var secondHeight = Math.Max(MinSecondSize, availableSize.Height * (1 - SplitterPosition) - SplitterSize / 2);

            first.Measure(new Size(availableSize.Width, firstHeight));
            second.Measure(new Size(availableSize.Width, secondHeight));
        }

        return availableSize;
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        if (Children.Count < 2)
        {
            return;
        }

        var first = Children[0];
        var second = Children[1];

        if (Direction == SplitDirection.Horizontal)
        {
            var firstWidth = Math.Max(MinFirstSize, contentRect.Width * SplitterPosition - SplitterSize / 2);

            first.Arrange(new Rect(
                contentRect.X, contentRect.Y,
                firstWidth, contentRect.Height
            ));

            second.Arrange(new Rect(
                contentRect.X + firstWidth + SplitterSize, contentRect.Y,
                Math.Max(0, contentRect.Width - firstWidth - SplitterSize), contentRect.Height
            ));
        }
        else
        {
            var firstHeight = Math.Max(MinFirstSize, contentRect.Height * SplitterPosition - SplitterSize / 2);

            first.Arrange(new Rect(
                contentRect.X, contentRect.Y,
                contentRect.Width, firstHeight
            ));

            second.Arrange(new Rect(
                contentRect.X, contentRect.Y + firstHeight + SplitterSize,
                contentRect.Width, Math.Max(0, contentRect.Height - firstHeight - SplitterSize)
            ));
        }
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        base.Paint(renderer);

        var splitterColor = IsSplitterHovered ? SplitterHoverColor : SplitterColor;
        var contentRect = LayoutRect.Deflate(Margin);

        if (Direction == SplitDirection.Horizontal)
        {
            var splitterX = contentRect.X + contentRect.Width * SplitterPosition - SplitterSize / 2;

            renderer.DrawRect(
                splitterX, contentRect.Y,
                SplitterSize, contentRect.Height,
                splitterColor.R, splitterColor.G, splitterColor.B, splitterColor.A
            );
        }
        else
        {
            var splitterY = contentRect.Y + contentRect.Height * SplitterPosition - SplitterSize / 2;

            renderer.DrawRect(
                contentRect.X, splitterY,
                contentRect.Width, SplitterSize,
                splitterColor.R, splitterColor.G, splitterColor.B, splitterColor.A
            );
        }
    }

    #endregion
}

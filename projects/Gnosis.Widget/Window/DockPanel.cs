using Gnosis.Widget.Element;
using Gnosis.Widget.Layout;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.Window;

public enum DockZone
{
    Left,
    Right,
    Top,
    Bottom,
    Center
}

public sealed class DockPanel : ContainerElement
{
    #region 属性

    public float SplitterSize { get; set; } = 4;

    public Color SplitterColor { get; set; } = new(0.25f, 0.25f, 0.28f, 1.0f);

    public Color SplitterHoverColor { get; set; } = new(0.35f, 0.55f, 0.90f, 1.0f);

    #endregion

    #region 布局方法

    protected override Size MeasureChildren(Size availableSize)
    {
        float maxFreeWidth = availableSize.Width;
        float maxFreeHeight = availableSize.Height;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (child is not DockLayoutInfo dockInfo)
            {
                child.Measure(availableSize);
                continue;
            }

            var docked = dockInfo.Child;

            switch (dockInfo.Zone)
            {
                case DockZone.Left:
                case DockZone.Right:
                    docked.Measure(new Size(maxFreeWidth, availableSize.Height));
                    maxFreeWidth -= docked.DesiredSize.Width + SplitterSize;
                    break;
                case DockZone.Top:
                case DockZone.Bottom:
                    docked.Measure(new Size(availableSize.Width, maxFreeHeight));
                    maxFreeHeight -= docked.DesiredSize.Height + SplitterSize;
                    break;
                case DockZone.Center:
                    docked.Measure(new Size(Math.Max(0, maxFreeWidth), Math.Max(0, maxFreeHeight)));
                    break;
            }
        }

        return new Size(availableSize.Width, availableSize.Height);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        float left = contentRect.X;
        float top = contentRect.Y;
        float right = contentRect.Right;
        float bottom = contentRect.Bottom;

        var centerChild = new List<DockLayoutInfo>();

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (child is not DockLayoutInfo dockInfo)
            {
                continue;
            }

            if (dockInfo.Zone == DockZone.Center)
            {
                centerChild.Add(dockInfo);
                continue;
            }

            var docked = dockInfo.Child;

            switch (dockInfo.Zone)
            {
                case DockZone.Left:
                    docked.Arrange(new Rect(left, top, docked.DesiredSize.Width, bottom - top));
                    left += docked.DesiredSize.Width + SplitterSize;
                    break;
                case DockZone.Right:
                    docked.Arrange(new Rect(right - docked.DesiredSize.Width, top, docked.DesiredSize.Width, bottom - top));
                    right -= docked.DesiredSize.Width + SplitterSize;
                    break;
                case DockZone.Top:
                    docked.Arrange(new Rect(left, top, right - left, docked.DesiredSize.Height));
                    top += docked.DesiredSize.Height + SplitterSize;
                    break;
                case DockZone.Bottom:
                    docked.Arrange(new Rect(left, bottom - docked.DesiredSize.Height, right - left, docked.DesiredSize.Height));
                    bottom -= docked.DesiredSize.Height + SplitterSize;
                    break;
            }
        }

        foreach (var info in centerChild)
        {
            info.Child.Arrange(new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top)));
        }
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        base.Paint(renderer);

        PaintSplitters(renderer);
    }

    private void PaintSplitters(IWidgetRenderer renderer)
    {
        float left = LayoutRect.X;
        float top = LayoutRect.Y;
        float right = LayoutRect.Right;
        float bottom = LayoutRect.Bottom;

        foreach (var child in Children)
        {
            if (child is not DockLayoutInfo dockInfo || child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            var docked = dockInfo.Child;
            var splitterColor = SplitterColor;

            switch (dockInfo.Zone)
            {
                case DockZone.Left:
                    renderer.DrawRect(
                        left + docked.DesiredSize.Width, top,
                        SplitterSize, bottom - top,
                        splitterColor.R, splitterColor.G, splitterColor.B, splitterColor.A
                    );
                    left += docked.DesiredSize.Width + SplitterSize;
                    break;
                case DockZone.Right:
                    renderer.DrawRect(
                        right - docked.DesiredSize.Width - SplitterSize, top,
                        SplitterSize, bottom - top,
                        splitterColor.R, splitterColor.G, splitterColor.B, splitterColor.A
                    );
                    right -= docked.DesiredSize.Width + SplitterSize;
                    break;
                case DockZone.Top:
                    renderer.DrawRect(
                        left, top + docked.DesiredSize.Height,
                        right - left, SplitterSize,
                        splitterColor.R, splitterColor.G, splitterColor.B, splitterColor.A
                    );
                    top += docked.DesiredSize.Height + SplitterSize;
                    break;
                case DockZone.Bottom:
                    renderer.DrawRect(
                        left, bottom - docked.DesiredSize.Height - SplitterSize,
                        right - left, SplitterSize,
                        splitterColor.R, splitterColor.G, splitterColor.B, splitterColor.A
                    );
                    bottom -= docked.DesiredSize.Height + SplitterSize;
                    break;
            }
        }
    }

    #endregion

    #region 公开方法

    public void Dock(WidgetElement child, DockZone zone)
    {
        var info = new DockLayoutInfo(child, zone);
        AddChild(info);
    }

    #endregion
}

internal sealed class DockLayoutInfo : WidgetElement
{
    public WidgetElement Child { get; }
    public DockZone Zone { get; }

    public DockLayoutInfo(WidgetElement child, DockZone zone)
    {
        Child = child;
        Zone = zone;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return Child.DesiredSize;
    }

    protected override void ArrangeOverride(Rect contentRect)
    {
    }

    public override void Paint(IWidgetRenderer renderer)
    {
        Child.Paint(renderer);
    }
}

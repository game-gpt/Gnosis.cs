namespace Gnosis.Rendering.Widget;

public sealed class LayoutEngine : ILayoutEngine
{
    public void Layout(Widget root, Size availableSize)
    {
        if (!IsDirty(root))
        {
            return;
        }

        root.Measure(availableSize);
        root.Arrange(new Rect(0, 0, availableSize.Width, availableSize.Height));
    }

    public void Layout(Widget root, float width, float height)
    {
        Layout(root, new Size(width, height));
    }

    void ILayoutEngine.Layout(IWidget root, WidgetSize availableSize)
    {
        if (root is Widget widget)
        {
            Layout(widget, new Size(availableSize.Width, availableSize.Height));
        }
    }

    void ILayoutEngine.Layout(IWidget root, float width, float height)
    {
        if (root is Widget widget)
        {
            Layout(widget, width, height);
        }
    }

    // 递归检查组件树中是否有脏标记
    private static bool IsDirty(Widget widget)
    {
        if (widget.IsMeasureDirty() || widget.IsArrangeDirty())
        {
            return true;
        }

        if (widget is ContainerWidget container)
        {
            foreach (var child in container.Children)
            {
                if (IsDirty(child))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

using GnosisEngine.UI.Layout;

namespace GnosisEngine.UI;

public sealed class LayoutEngine
{
    public void Layout(Widget root, Size availableSize)
    {
        root.Measure(availableSize);
        root.Arrange(new Rect(0, 0, availableSize.Width, availableSize.Height));
    }

    public void Layout(Widget root, float width, float height)
    {
        Layout(root, new Size(width, height));
    }
}

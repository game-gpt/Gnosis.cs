using Gnosis.Widget.Element;

namespace Gnosis.Widget.Render;

public interface ILayoutEngine
{
    void Layout(WidgetElement root, Size availableSize);
    void Layout(WidgetElement root, float width, float height);
}

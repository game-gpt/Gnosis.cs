using Gnosis.Widget.Element;

namespace Gnosis.Widget.Render;

public interface IWidgetTreeRenderer
{
    void Render(WidgetElement root, float width, float height);
}

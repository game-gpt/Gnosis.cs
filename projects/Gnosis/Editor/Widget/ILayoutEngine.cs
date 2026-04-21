namespace Gnosis.Editor.Widget;

public interface ILayoutEngine
{
    void Layout(IWidget root, WidgetSize availableSize);
    void Layout(IWidget root, float width, float height);
}

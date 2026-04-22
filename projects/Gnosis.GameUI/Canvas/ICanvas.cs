using Gnosis.GameUI.Element;
using Gnosis.GameUI.Render;

namespace Gnosis.GameUI.Canvas;

public interface ICanvas
{
    UIRenderMode RenderMode { get; set; }
    int SortingOrder { get; set; }
    IReadOnlyList<IUIElement> Elements { get; }

    void AddElement(IUIElement element);
    void RemoveElement(IUIElement element);
    void ClearElements();
}

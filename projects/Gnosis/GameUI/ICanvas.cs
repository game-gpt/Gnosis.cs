using Gnosis.GameUI.Enums;

namespace Gnosis.GameUI;

public interface ICanvas
{
    UIRenderMode RenderMode { get; set; }
    int SortingOrder { get; set; }
    IReadOnlyList<IUIElement> Elements { get; }

    void AddElement(IUIElement element);
    void RemoveElement(IUIElement element);
    void ClearElements();
}

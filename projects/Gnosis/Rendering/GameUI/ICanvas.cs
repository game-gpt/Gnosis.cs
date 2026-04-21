namespace Gnosis.Rendering.GameUI;

public interface ICanvas
{
    UIRenderMode RenderMode { get; set; }
    int SortingOrder { get; set; }
    IReadOnlyList<IUIElement> Elements { get; }

    void AddElement(IUIElement element);
    void RemoveElement(IUIElement element);
    void ClearElements();
}

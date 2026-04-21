namespace Gnosis.Rendering.GameUI;

public sealed class Canvas : ICanvas
{
    #region Fields

    private readonly List<IUIElement> _elements = [];

    #endregion

    #region Properties

    public UIRenderMode RenderMode { get; set; } = UIRenderMode.ScreenSpace;

    public int SortingOrder { get; set; } = 0;

    public IReadOnlyList<IUIElement> Elements => _elements;

    #endregion

    #region Public Methods

    public void AddElement(IUIElement element)
    {
        _elements.Add(element);
    }

    public void RemoveElement(IUIElement element)
    {
        _elements.Remove(element);
    }

    public void ClearElements()
    {
        _elements.Clear();
    }

    #endregion
}

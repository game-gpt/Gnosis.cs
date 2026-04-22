namespace Gnosis.Widget.Element;

public interface IContainerElement : IWidgetElement
{
    IReadOnlyList<IWidgetElement> Children { get; }

    void AddChild(IWidgetElement child);
    void RemoveChild(IWidgetElement child);
    void ClearChildren();
}

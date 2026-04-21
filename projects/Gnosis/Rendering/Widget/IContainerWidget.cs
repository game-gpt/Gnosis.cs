namespace Gnosis.Rendering.Widget;

public interface IContainerWidget : IWidget
{
    IReadOnlyList<IWidget> Children { get; }

    void AddChild(IWidget child);
    void RemoveChild(IWidget child);
    void ClearChildren();
}

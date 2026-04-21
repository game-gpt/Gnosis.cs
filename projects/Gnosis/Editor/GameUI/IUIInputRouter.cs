namespace Gnosis.Editor.GameUI;

public interface IUIInputRouter
{
    bool ProcessEvent(object inputEvent);
    void RegisterHandler(UIInputLayer layer, Func<object, bool> handler);
    void UnregisterHandler(UIInputLayer layer, Func<object, bool> handler);
}

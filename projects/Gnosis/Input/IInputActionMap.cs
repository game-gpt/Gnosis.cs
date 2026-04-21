namespace Gnosis.Input;

public interface IInputActionMap
{
    string Name { get; }
    bool IsEnabled { get; set; }
    IReadOnlyList<IInputAction> Actions { get; }
    IInputAction GetAction(string name);
    void AddAction(IInputAction action);
    void RemoveAction(string name);
    void Enable();
    void Disable();
}

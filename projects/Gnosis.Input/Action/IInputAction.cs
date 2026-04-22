using Gnosis.Input.Binding;
using Gnosis.Input.Simulate;

namespace Gnosis.Input.Action;

public interface IInputAction
{
    string Name { get; }
    bool IsEnabled { get; set; }
    IReadOnlyList<IInputBinding> Bindings { get; }
    void AddBinding(IInputBinding binding);
    void RemoveBinding(string bindingName);
    event Action<IInputContext>? OnStarted;
    event Action<IInputContext>? OnPerformed;
    event Action<IInputContext>? OnCancelled;
}

using Gnosis.Input.Device;

namespace Gnosis.Input.Binding;

public interface IInputBinding
{
    string Name { get; }
    string Path { get; }
    InputDeviceType DeviceType { get; }
    bool IsComposite { get; }
    IReadOnlyList<IInputBinding> CompositeBindings { get; }
}

namespace Gnosis.Input.Interface;

public interface IInputBinding
{
    string Name { get; }
    string Path { get; }
    InputDeviceType DeviceType { get; }
    bool IsComposite { get; }
    IReadOnlyList<IInputBinding> CompositeBindings { get; }
}

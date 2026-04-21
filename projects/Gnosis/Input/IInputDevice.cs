namespace Gnosis.Input;

public interface IInputDevice
{
    string Name { get; }
    InputDeviceType DeviceType { get; }
    bool IsConnected { get; }
    void Update();
    void Reset();
}

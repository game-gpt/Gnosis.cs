using Gnosis.Input.Action;
using Gnosis.Input.Device;

namespace Gnosis.Input.Simulate;

public interface IInputSystem
{
    IReadOnlyList<IInputDevice> Devices { get; }
    IKeyboard? Keyboard { get; }
    IMouse? Mouse { get; }
    IReadOnlyList<IGamepad> Gamepads { get; }
    ITouch? Touch { get; }
    IInputActionMap CreateActionMap(string name);
    void DestroyActionMap(string name);
    IInputActionMap? GetActionMap(string name);
    void EnableActionMap(string name);
    void DisableActionMap(string name);
    void OnDeviceConnected(IInputDevice device);
    void OnDeviceDisconnected(IInputDevice device);
    void Update();
}

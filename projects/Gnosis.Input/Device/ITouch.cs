namespace Gnosis.Input.Device;

public interface ITouch : IInputDevice
{
    int TouchCount { get; }
    TouchPoint GetTouch(int index);
}
namespace Gnosis.Input.Touch;

public interface ITouch : IInputDevice
{
    int TouchCount { get; }
    TouchPoint GetTouch(int index);
}
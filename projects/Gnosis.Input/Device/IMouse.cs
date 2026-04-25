using System.Numerics;

namespace Gnosis.Input.Device;

public interface IMouse : IInputDevice
{
    Vector2 Position { get; }
    Vector2 Delta { get; }
    float ScrollDelta { get; }
    bool GetButton(int buttonIndex);
    bool GetButtonDown(int buttonIndex);
    bool GetButtonUp(int buttonIndex);
}

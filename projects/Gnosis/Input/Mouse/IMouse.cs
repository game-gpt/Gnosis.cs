namespace Gnosis.Input.Mouse;

public interface IMouse : IInputDevice
{
    float[] Position { get; }
    float[] Delta { get; }
    float ScrollDelta { get; }
    bool GetButton(int buttonIndex);
    bool GetButtonDown(int buttonIndex);
    bool GetButtonUp(int buttonIndex);
}

namespace Gnosis.Input.Gamepad;

public interface IGamepad : IInputDevice
{
    int PlayerIndex { get; }
    float GetAxis(int axisIndex);
    float GetAxisRaw(int axisIndex);
    bool GetButton(int buttonIndex);
    bool GetButtonDown(int buttonIndex);
    bool GetButtonUp(int buttonIndex);
    void SetVibration(float leftMotor, float rightMotor);
}

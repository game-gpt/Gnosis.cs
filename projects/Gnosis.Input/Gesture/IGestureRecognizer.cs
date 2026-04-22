using Gnosis.Input.Device;

namespace Gnosis.Input.Gesture;

public interface IGestureRecognizer
{
    bool IsEnabled { get; set; }

    void ProcessTouch(in TouchPoint touch);

    void ProcessMouse(float[] position, bool isPressed);

    void Reset();
}

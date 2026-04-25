using System.Numerics;
using Gnosis.Input.Device;

namespace Gnosis.Input.Gesture;

public interface IGestureRecognizer
{
    bool IsEnabled { get; set; }

    void ProcessTouch(in TouchPoint touch);

    void ProcessMouse(Vector2 position, bool isPressed);

    void Reset();
}

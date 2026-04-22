using Gnosis.Input.Action;
using Gnosis.Input.Device;

namespace Gnosis.Input.Simulate;

public sealed class InputContext : IInputContext
{
    #region 属性

    public IInputAction Action { get; }

    public IInputDevice Device { get; }

    public float Value { get; }

    public bool IsPressed { get; }

    public float Duration { get; internal set; }

    public float StartTime { get; }

    #endregion

    #region 构造函数

    public InputContext(IInputAction action, IInputDevice device, float value, bool isPressed, float startTime)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Device = device ?? throw new ArgumentNullException(nameof(device));
        Value = value;
        IsPressed = isPressed;
        StartTime = startTime;
        Duration = 0f;
    }

    #endregion
}

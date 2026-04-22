using System.Runtime.CompilerServices;

namespace Gnosis.Input.Device;

public sealed class GamepadDevice : IGamepad
{
    #region 常量

    private const int ButtonCount = 32;
    private const int AxisCount = 8;
    private const float DefaultDeadzone = 0.1f;

    #endregion

    #region 字段

    private readonly float[] _axes = new float[AxisCount];
    private readonly bool[] _currentButtons = new bool[ButtonCount];
    private readonly bool[] _previousButtons = new bool[ButtonCount];
    private float _leftMotorVibration;
    private float _rightMotorVibration;

    #endregion

    #region 属性

    public string Name => $"Gamepad {PlayerIndex}";

    public InputDeviceType DeviceType => InputDeviceType.Gamepad;

    public bool IsConnected { get; set; }

    public int PlayerIndex { get; }

    public float Deadzone { get; set; } = DefaultDeadzone;

    #endregion

    #region 构造函数

    public GamepadDevice(int playerIndex)
    {
        PlayerIndex = playerIndex;
    }

    #endregion

    #region IGamepad 实现

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetAxis(int axisIndex)
    {
        if (axisIndex < 0 || axisIndex >= AxisCount)
        {
            return 0f;
        }

        var value = _axes[axisIndex];

        if (MathF.Abs(value) < Deadzone)
        {
            return 0f;
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetAxisRaw(int axisIndex)
    {
        if (axisIndex < 0 || axisIndex >= AxisCount)
        {
            return 0f;
        }

        return _axes[axisIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetButton(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= ButtonCount)
        {
            return false;
        }

        return _currentButtons[buttonIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetButtonDown(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= ButtonCount)
        {
            return false;
        }

        return _currentButtons[buttonIndex] && !_previousButtons[buttonIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetButtonUp(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= ButtonCount)
        {
            return false;
        }

        return !_currentButtons[buttonIndex] && _previousButtons[buttonIndex];
    }

    public void SetVibration(float leftMotor, float rightMotor)
    {
        _leftMotorVibration = Math.Clamp(leftMotor, 0f, 1f);
        _rightMotorVibration = Math.Clamp(rightMotor, 0f, 1f);
    }

    #endregion

    #region 内部方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAxis(int axisIndex, float value)
    {
        if (axisIndex < 0 || axisIndex >= AxisCount)
        {
            return;
        }

        _axes[axisIndex] = Math.Clamp(value, -1f, 1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetButtonState(int buttonIndex, bool isPressed)
    {
        if (buttonIndex < 0 || buttonIndex >= ButtonCount)
        {
            return;
        }

        _currentButtons[buttonIndex] = isPressed;
    }

    public float GetLeftMotorVibration() => _leftMotorVibration;

    public float GetRightMotorVibration() => _rightMotorVibration;

    #endregion

    #region IInputDevice 实现

    public void Update()
    {
        for (var i = 0; i < ButtonCount; i++)
        {
            _previousButtons[i] = _currentButtons[i];
        }
    }

    public void Reset()
    {
        Array.Clear(_axes);
        Array.Clear(_currentButtons);
        Array.Clear(_previousButtons);
        _leftMotorVibration = 0f;
        _rightMotorVibration = 0f;
    }

    #endregion
}

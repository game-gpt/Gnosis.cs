using System.Numerics;
using System.Runtime.CompilerServices;

namespace Gnosis.Input.Device;

public sealed class Mouse : IMouse
{
    #region 常量

    private const int ButtonCount = 8;

    #endregion

    #region 字段

    private readonly bool[] _currentButtons = new bool[ButtonCount];
    private readonly bool[] _previousButtons = new bool[ButtonCount];
    private Vector2 _position = Vector2.Zero;
    private Vector2 _previousPosition = Vector2.Zero;
    private Vector2 _delta = Vector2.Zero;
    private float _scrollDelta;
    private float _previousScroll;

    #endregion

    #region 属性

    public string Name => "Mouse";

    public InputDeviceType DeviceType => InputDeviceType.Mouse;

    public bool IsConnected { get; set; } = true;

    public Vector2 Position => _position;

    public Vector2 Delta => _delta;

    public float ScrollDelta => _scrollDelta;

    #endregion

    #region IMouse 实现

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

    #endregion

    #region 内部方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPosition(float x, float y)
    {
        _previousPosition = _position;
        _position = new Vector2(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetScrollDelta(float delta)
    {
        _previousScroll = _scrollDelta;
        _scrollDelta = delta;
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

    #endregion

    #region IInputDevice 实现

    public void Update()
    {
        for (var i = 0; i < ButtonCount; i++)
        {
            _previousButtons[i] = _currentButtons[i];
        }

        _delta = _position - _previousPosition;
        _scrollDelta -= _previousScroll;
    }

    public void Reset()
    {
        Array.Clear(_currentButtons);
        Array.Clear(_previousButtons);
        _position = Vector2.Zero;
        _previousPosition = Vector2.Zero;
        _delta = Vector2.Zero;
        _scrollDelta = 0f;
        _previousScroll = 0f;
    }

    #endregion
}

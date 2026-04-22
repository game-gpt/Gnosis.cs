using System.Runtime.CompilerServices;

namespace Gnosis.Input.Device;

public sealed class Keyboard : IKeyboard
{
    #region 常量

    private const int KeyCount = 256;

    #endregion

    #region 字段

    private readonly bool[] _currentKeys = new bool[KeyCount];
    private readonly bool[] _previousKeys = new bool[KeyCount];
    private readonly List<int> _currentKeyList = new();

    #endregion

    #region 属性

    public string Name => "Keyboard";

    public InputDeviceType DeviceType => InputDeviceType.Keyboard;

    public bool IsConnected { get; set; } = true;

    #endregion

    #region IKeyboard 实现

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetKey(int keyCode)
    {
        if (keyCode < 0 || keyCode >= KeyCount)
        {
            return false;
        }

        return _currentKeys[keyCode];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetKeyDown(int keyCode)
    {
        if (keyCode < 0 || keyCode >= KeyCount)
        {
            return false;
        }

        return _currentKeys[keyCode] && !_previousKeys[keyCode];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetKeyUp(int keyCode)
    {
        if (keyCode < 0 || keyCode >= KeyCount)
        {
            return false;
        }

        return !_currentKeys[keyCode] && _previousKeys[keyCode];
    }

    public IReadOnlyList<int> GetCurrentKeys()
    {
        return _currentKeyList;
    }

    #endregion

    #region 内部方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetKeyState(int keyCode, bool isPressed)
    {
        if (keyCode < 0 || keyCode >= KeyCount)
        {
            return;
        }

        _currentKeys[keyCode] = isPressed;
    }

    #endregion

    #region IInputDevice 实现

    public void Update()
    {
        _currentKeyList.Clear();

        for (var i = 0; i < KeyCount; i++)
        {
            _previousKeys[i] = _currentKeys[i];

            if (_currentKeys[i])
            {
                _currentKeyList.Add(i);
            }
        }
    }

    public void Reset()
    {
        Array.Clear(_currentKeys);
        Array.Clear(_previousKeys);
        _currentKeyList.Clear();
    }

    #endregion
}

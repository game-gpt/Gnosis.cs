using System.Diagnostics;
using Gnosis.Input.Action;
using Gnosis.Input.Device;

namespace Gnosis.Input.Simulate;

public sealed class InputSystem : IInputSystem
{
    #region 字段

    private readonly List<IInputDevice> _devices = new();
    private readonly Dictionary<string, IInputActionMap> _actionMaps = new();
    private readonly List<IInputActionMap> _actionMapList = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly List<IGamepad> _gamepads = new();

    #endregion

    #region 属性

    public IReadOnlyList<IInputDevice> Devices => _devices;

    public IKeyboard? Keyboard { get; private set; }

    public IMouse? Mouse { get; private set; }

    public IReadOnlyList<IGamepad> Gamepads => _gamepads;

    public ITouch? Touch { get; private set; }

    #endregion

    #region 构造函数

    public InputSystem()
    {
        _stopwatch.Start();
        InitializeDefaultDevices();
    }

    #endregion

    #region IInputSystem 实现

    public IInputActionMap CreateActionMap(string name)
    {
        if (_actionMaps.ContainsKey(name))
        {
            throw new ArgumentException($"动作映射已存在：{name}", nameof(name));
        }

        var actionMap = new InputActionMap(name);
        _actionMaps[name] = actionMap;
        _actionMapList.Add(actionMap);

        return actionMap;
    }

    public void DestroyActionMap(string name)
    {
        if (_actionMaps.TryGetValue(name, out var actionMap))
        {
            _actionMaps.Remove(name);
            _actionMapList.Remove(actionMap);
        }
    }

    public IInputActionMap? GetActionMap(string name)
    {
        return _actionMaps.GetValueOrDefault(name);
    }

    public void EnableActionMap(string name)
    {
        if (_actionMaps.TryGetValue(name, out var actionMap))
        {
            actionMap.Enable();
        }
    }

    public void DisableActionMap(string name)
    {
        if (_actionMaps.TryGetValue(name, out var actionMap))
        {
            actionMap.Disable();
        }
    }

    public void OnDeviceConnected(IInputDevice device)
    {
        if (device is null)
        {
            throw new ArgumentNullException(nameof(device));
        }

        _devices.Add(device);

        switch (device)
        {
            case IKeyboard keyboard:
                Keyboard = keyboard;
                break;
            case IMouse mouse:
                Mouse = mouse;
                break;
            case IGamepad gamepad:
                _gamepads.Add(gamepad);
                break;
            case ITouch touch:
                Touch = touch;
                break;
        }
    }

    public void OnDeviceDisconnected(IInputDevice device)
    {
        if (device is null)
        {
            return;
        }

        _devices.Remove(device);

        switch (device)
        {
            case IKeyboard:
                if (Keyboard == device)
                {
                    Keyboard = null;
                }

                break;
            case IMouse:
                if (Mouse == device)
                {
                    Mouse = null;
                }

                break;
            case IGamepad gamepad:
                _gamepads.Remove(gamepad);
                break;
            case ITouch:
                if (Touch == device)
                {
                    Touch = null;
                }

                break;
        }
    }

    public void Update()
    {
        foreach (var device in _devices)
        {
            device.Update();
        }

        var currentTime = (float)_stopwatch.Elapsed.TotalSeconds;

        foreach (var actionMap in _actionMapList)
        {
            if (!actionMap.IsEnabled)
            {
                continue;
            }

            foreach (var action in actionMap.Actions)
            {
                if (!action.IsEnabled)
                {
                    continue;
                }

                if (action is InputAction concreteAction)
                {
                    foreach (var device in _devices)
                    {
                        concreteAction.ProcessInput(device, currentTime);
                    }
                }
            }
        }
    }

    #endregion

    #region 私有方法

    private void InitializeDefaultDevices()
    {
        var keyboard = new Keyboard();
        var mouse = new Mouse();
        var touch = new TouchDevice();

        OnDeviceConnected(keyboard);
        OnDeviceConnected(mouse);
        OnDeviceConnected(touch);
    }

    #endregion
}

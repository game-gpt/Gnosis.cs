using System.Diagnostics;
using Gnosis.Input.Binding;
using Gnosis.Input.Device;
using Gnosis.Input.Simulate;

namespace Gnosis.Input.Action;

public sealed class InputAction : IInputAction
{
    #region 字段

    private readonly List<IInputBinding> _bindings = new();
    private readonly Dictionary<string, IInputBinding> _bindingsByName = new();
    private readonly Stopwatch _stopwatch = new();
    private bool _isActive;
    private float _startTime;

    #endregion

    #region 属性

    public string Name { get; }

    public bool IsEnabled { get; set; } = true;

    public IReadOnlyList<IInputBinding> Bindings => _bindings;

    #endregion

    #region 事件

    public event Action<IInputContext>? OnStarted;
    public event Action<IInputContext>? OnPerformed;
    public event Action<IInputContext>? OnCancelled;

    #endregion

    #region 构造函数

    public InputAction(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _stopwatch.Start();
    }

    #endregion

    #region IInputAction 实现

    public void AddBinding(IInputBinding binding)
    {
        if (binding is null)
        {
            throw new ArgumentNullException(nameof(binding));
        }

        _bindings.Add(binding);
        _bindingsByName[binding.Name] = binding;
    }

    public void RemoveBinding(string bindingName)
    {
        if (_bindingsByName.TryGetValue(bindingName, out var binding))
        {
            _bindings.Remove(binding);
            _bindingsByName.Remove(bindingName);
        }
    }

    #endregion

    #region 内部方法

    internal void ProcessInput(IInputDevice device, float currentTime)
    {
        if (!IsEnabled)
        {
            return;
        }

        var wasActive = _isActive;
        var value = 0f;
        var isPressed = false;

        foreach (var binding in _bindings)
        {
            if (binding.DeviceType != device.DeviceType)
            {
                continue;
            }

            var (bindingValue, bindingPressed) = EvaluateBinding(binding, device);

            if (bindingPressed)
            {
                isPressed = true;
                value = MathF.Max(value, bindingValue);
            }
        }

        _isActive = isPressed;

        if (!wasActive && isPressed)
        {
            _startTime = currentTime;
            var context = new InputContext(this, device, value, true, _startTime);
            OnStarted?.Invoke(context);
            OnPerformed?.Invoke(context);
        }
        else if (wasActive && isPressed)
        {
            var context = new InputContext(this, device, value, true, _startTime)
            {
                Duration = currentTime - _startTime
            };
            OnPerformed?.Invoke(context);
        }
        else if (wasActive && !isPressed)
        {
            var context = new InputContext(this, device, 0f, false, _startTime)
            {
                Duration = currentTime - _startTime
            };
            OnCancelled?.Invoke(context);
        }
    }

    #endregion

    #region 私有方法

    private static (float value, bool pressed) EvaluateBinding(IInputBinding binding, IInputDevice device)
    {
        if (binding.IsComposite && binding.CompositeBindings.Count > 0)
        {
            return EvaluateCompositeBinding(binding, device);
        }

        return device.DeviceType switch
        {
            InputDeviceType.Keyboard => EvaluateKeyboardBinding(binding, device),
            InputDeviceType.Mouse => EvaluateMouseBinding(binding, device),
            InputDeviceType.Gamepad => EvaluateGamepadBinding(binding, device),
            _ => (0f, false)
        };
    }

    private static (float value, bool pressed) EvaluateCompositeBinding(IInputBinding binding, IInputDevice device)
    {
        foreach (var child in binding.CompositeBindings)
        {
            var (value, pressed) = EvaluateBinding(child, device);

            if (pressed)
            {
                return (value, true);
            }
        }

        return (0f, false);
    }

    private static (float value, bool pressed) EvaluateKeyboardBinding(IInputBinding binding, IInputDevice device)
    {
        if (device is not IKeyboard keyboard)
        {
            return (0f, false);
        }

        var keyCode = ParseBindingPath(binding.Path);

        if (keyCode < 0)
        {
            return (0f, false);
        }

        var isPressed = keyboard.GetKey(keyCode);
        return (isPressed ? 1f : 0f, isPressed);
    }

    private static (float value, bool pressed) EvaluateMouseBinding(IInputBinding binding, IInputDevice device)
    {
        if (device is not IMouse mouse)
        {
            return (0f, false);
        }

        var path = binding.Path.ToLowerInvariant();

        if (path.Contains("button"))
        {
            var buttonIndex = ParseBindingPath(binding.Path);
            var isPressed = mouse.GetButton(buttonIndex);
            return (isPressed ? 1f : 0f, isPressed);
        }

        if (path.Contains("scroll"))
        {
            var scrollDelta = mouse.ScrollDelta;
            return (scrollDelta, MathF.Abs(scrollDelta) > 0.001f);
        }

        return (0f, false);
    }

    private static (float value, bool pressed) EvaluateGamepadBinding(IInputBinding binding, IInputDevice device)
    {
        if (device is not IGamepad gamepad)
        {
            return (0f, false);
        }

        var path = binding.Path.ToLowerInvariant();

        if (path.Contains("axis") || path.Contains("stick") || path.Contains("trigger"))
        {
            var axisIndex = ParseBindingPath(binding.Path);
            var axisValue = gamepad.GetAxis(axisIndex);
            return (axisValue, MathF.Abs(axisValue) > 0.01f);
        }

        if (path.Contains("button"))
        {
            var buttonIndex = ParseBindingPath(binding.Path);
            var isPressed = gamepad.GetButton(buttonIndex);
            return (isPressed ? 1f : 0f, isPressed);
        }

        return (0f, false);
    }

    private static int ParseBindingPath(string path)
    {
        var lastSlash = path.LastIndexOf('/');

        if (lastSlash < 0)
        {
            return -1;
        }

        var part = path[(lastSlash + 1)..];

        return int.TryParse(part, out var result) ? result : -1;
    }

    #endregion
}

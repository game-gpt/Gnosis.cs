using Gnosis.Input.Interface;

namespace Gnosis.Input.Implementation;

public class StubInputSystem : IInputSystem
{
    public IReadOnlyList<IInputDevice> Devices => throw new NotImplementedException("输入系统尚未实现");

    public IKeyboard? Keyboard => throw new NotImplementedException("输入系统尚未实现");

    public IMouse? Mouse => throw new NotImplementedException("输入系统尚未实现");

    public IReadOnlyList<IGamepad> Gamepads => throw new NotImplementedException("输入系统尚未实现");

    public ITouch? Touch => throw new NotImplementedException("输入系统尚未实现");

    public IInputActionMap CreateActionMap(string name)
    {
        throw new NotImplementedException("输入系统尚未实现");
    }

    public void DestroyActionMap(string name)
    {
        throw new NotImplementedException("输入系统尚未实现");
    }

    public IInputActionMap? GetActionMap(string name)
    {
        throw new NotImplementedException("输入系统尚未实现");
    }

    public void EnableActionMap(string name)
    {
        throw new NotImplementedException("输入系统尚未实现");
    }

    public void DisableActionMap(string name)
    {
        throw new NotImplementedException("输入系统尚未实现");
    }

    public void OnDeviceConnected(IInputDevice device)
    {
        throw new NotImplementedException("输入系统尚未实现");
    }

    public void OnDeviceDisconnected(IInputDevice device)
    {
        throw new NotImplementedException("输入系统尚未实现");
    }

    public void Update()
    {
        throw new NotImplementedException("输入系统尚未实现");
    }
}

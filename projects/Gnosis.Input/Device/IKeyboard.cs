namespace Gnosis.Input.Device;

public interface IKeyboard : IInputDevice
{
    bool GetKey(int keyCode);
    bool GetKeyDown(int keyCode);
    bool GetKeyUp(int keyCode);
    IReadOnlyList<int> GetCurrentKeys();
}

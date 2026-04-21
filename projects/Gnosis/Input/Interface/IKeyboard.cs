namespace Gnosis.Input.Interface;

public interface IKeyboard : IInputDevice
{
    bool GetKey(int keyCode);
    bool GetKeyDown(int keyCode);
    bool GetKeyUp(int keyCode);
    IReadOnlyList<int> GetCurrentKeys();
}

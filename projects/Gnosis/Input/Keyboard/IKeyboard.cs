namespace Gnosis.Input.Keyboard;

public interface IKeyboard : IInputDevice
{
    bool GetKey(int keyCode);
    bool GetKeyDown(int keyCode);
    bool GetKeyUp(int keyCode);
    IReadOnlyList<int> GetCurrentKeys();
}

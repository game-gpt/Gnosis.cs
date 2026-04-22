namespace Gnosis.Runtime.VM;

public interface IVMState
{
    int IP { get; set; }
    int SP { get; set; }
    object?[] Stack { get; }
    IReadOnlyList<IModule> Modules { get; }
    IModule? CurrentModule { get; }
    void Push(object? value);
    object? Pop();
    object? Peek();
    void LoadModule(IModule module);
    void UnloadModule(string name);
    IModule? GetModule(string name);
    void Reset();
    IReadOnlyList<CallFrameInfo> CallFrames { get; }
    VMStateSnapshot CreateSnapshot();
}

namespace Gnosis.Runtime.VM;

public interface IVMState
{
    int IP { get; set; }
    int SP { get; set; }
    GGValue[] Stack { get; }
    IReadOnlyList<IModule> Modules { get; }
    IModule? CurrentModule { get; }
    void Push(GGValue value);
    GGValue Pop();
    GGValue Peek();
    void LoadModule(IModule module);
    void UnloadModule(string name);
    IModule? GetModule(string name);
    void Reset();
    IReadOnlyList<CallFrameInfo> CallFrames { get; }
    VMStateSnapshot CreateSnapshot();
}

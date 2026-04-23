namespace Gnosis.Runtime.VM;

/// <summary>
/// 虚拟机状态快照
/// </summary>
public sealed class VMStateSnapshot
{
    #region 属性

    /// <summary>
    /// 指令指针
    /// </summary>
    public int IP { get; init; }

    /// <summary>
    /// 栈指针
    /// </summary>
    public int SP { get; init; }

    /// <summary>
    /// 操作数栈快照
    /// </summary>
    public GGValue[] Stack { get; init; }

    /// <summary>
    /// 调用帧快照
    /// </summary>
    public CallFrameInfo[] CallFrames { get; init; }

    /// <summary>
    /// 当前模块名称
    /// </summary>
    public string? CurrentModuleName { get; init; }

    /// <summary>
    /// 模块数量
    /// </summary>
    public int ModuleCount { get; init; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化空快照
    /// </summary>
    public VMStateSnapshot()
    {
        Stack = [];
        CallFrames = [];
    }

    /// <summary>
    /// 使用完整参数初始化快照
    /// </summary>
    public VMStateSnapshot(
        int ip,
        int sp,
        GGValue[] stack,
        CallFrameInfo[] callFrames,
        string? currentModuleName,
        int moduleCount)
    {
        IP = ip;
        SP = sp;
        Stack = stack;
        CallFrames = callFrames;
        CurrentModuleName = currentModuleName;
        ModuleCount = moduleCount;
    }

    #endregion
}

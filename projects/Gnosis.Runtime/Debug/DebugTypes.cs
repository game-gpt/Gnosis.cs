namespace Gnosis.Runtime.Debug;

/// <summary>
/// 断点信息
/// </summary>
public sealed class Breakpoint
{
    #region Properties

    public int Id { get; }
    public string ModuleName { get; }
    public int InstructionOffset { get; }
    public bool IsEnabled { get; set; }
    public int HitCount { get; private set; }
    public int? HitCondition { get; set; }

    #endregion

    #region Constructors

    public Breakpoint(int id, string moduleName, int instructionOffset)
    {
        Id = id;
        ModuleName = moduleName;
        InstructionOffset = instructionOffset;
        IsEnabled = true;
        HitCount = 0;
        HitCondition = null;
    }

    #endregion

    #region 方法

    /// <summary>
    /// 命中断点，返回是否应暂停
    /// </summary>
    public bool Hit()
    {
        HitCount++;

        if (!IsEnabled)
        {
            return false;
        }

        if (HitCondition.HasValue && HitCount < HitCondition.Value)
        {
            return false;
        }

        return true;
    }

    #endregion
}

/// <summary>
/// 调试事件类型
/// </summary>
public enum DebugEventType
{
    BreakpointHit,
    StepComplete,
    ExceptionThrown,
    ModuleLoaded,
    ModuleUnloaded,
    PauseRequested
}

/// <summary>
/// 调试事件参数
/// </summary>
public sealed class DebugEventArgs : EventArgs
{
    #region Properties

    public DebugEventType EventType { get; }
    public Breakpoint? Breakpoint { get; }
    public string? Message { get; }
    public VM.VMStateSnapshot Snapshot { get; }

    #endregion

    #region Constructors

    public DebugEventArgs(DebugEventType eventType, VM.VMStateSnapshot snapshot,
        Breakpoint? breakpoint = null, string? message = null)
    {
        EventType = eventType;
        Snapshot = snapshot;
        Breakpoint = breakpoint;
        Message = message;
    }

    #endregion
}

/// <summary>
/// 单步模式
/// </summary>
public enum StepMode
{
    None,
    StepOver,
    StepInto,
    StepOut
}

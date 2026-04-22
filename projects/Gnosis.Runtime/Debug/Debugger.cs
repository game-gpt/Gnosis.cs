namespace Gnosis.Runtime.Debug;

/// <summary>
/// 虚拟机调试器，支持断点、单步执行、变量监视
/// </summary>
public sealed class Debugger
{
    #region Fields

    private readonly VM.VMState _vmState;
    private readonly Dictionary<int, Breakpoint> _breakpoints = new();
    private readonly Dictionary<(string, int), Breakpoint> _breakpointLookup = new();
    private readonly List<VariableWatch> _watches = new();
    private int _nextBreakpointId;

    private bool _isPaused;
    private bool _isAttached;
    private StepMode _stepMode;
    private int _stepDepth;
    private int _currentDepth;

    #endregion

    #region Properties

    /// <summary>
    /// 调试器是否已附加
    /// </summary>
    public bool IsAttached => _isAttached;

    /// <summary>
    /// 虚拟机是否暂停
    /// </summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// 当前单步模式
    /// </summary>
    public StepMode CurrentStepMode => _stepMode;

    /// <summary>
    /// 所有断点
    /// </summary>
    public IReadOnlyList<Breakpoint> Breakpoints => _breakpoints.Values.ToList();

    /// <summary>
    /// 所有变量监视
    /// </summary>
    public IReadOnlyList<VariableWatch> Watches => _watches;

    #endregion

    #region Events

    /// <summary>
    /// 调试事件
    /// </summary>
    public event EventHandler<DebugEventArgs>? DebugEvent;

    #endregion

    #region Constructors

    public Debugger(VM.VMState vmState)
    {
        _vmState = vmState;
        _nextBreakpointId = 1;
    }

    #endregion

    #region 附加/分离

    /// <summary>
    /// 附加调试器
    /// </summary>
    public void Attach()
    {
        _isAttached = true;
        _isPaused = false;
        _stepMode = StepMode.None;
    }

    /// <summary>
    /// 分离调试器
    /// </summary>
    public void Detach()
    {
        _isAttached = false;
        _isPaused = false;
        _stepMode = StepMode.None;
    }

    #endregion

    #region 断点管理

    /// <summary>
    /// 添加断点
    /// </summary>
    public Breakpoint AddBreakpoint(string moduleName, int instructionOffset)
    {
        var key = (moduleName, instructionOffset);

        if (_breakpointLookup.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var bp = new Breakpoint(_nextBreakpointId++, moduleName, instructionOffset);
        _breakpoints[bp.Id] = bp;
        _breakpointLookup[key] = bp;
        return bp;
    }

    /// <summary>
    /// 移除断点
    /// </summary>
    public void RemoveBreakpoint(int breakpointId)
    {
        if (_breakpoints.TryGetValue(breakpointId, out var bp))
        {
            _breakpointLookup.Remove((bp.ModuleName, bp.InstructionOffset));
            _breakpoints.Remove(breakpointId);
        }
    }

    /// <summary>
    /// 启用/禁用断点
    /// </summary>
    public void SetBreakpointEnabled(int breakpointId, bool enabled)
    {
        if (_breakpoints.TryGetValue(breakpointId, out var bp))
        {
            bp.IsEnabled = enabled;
        }
    }

    /// <summary>
    /// 设置断点命中条件
    /// </summary>
    public void SetHitCondition(int breakpointId, int hitCount)
    {
        if (_breakpoints.TryGetValue(breakpointId, out var bp))
        {
            bp.HitCondition = hitCount;
        }
    }

    /// <summary>
    /// 清除所有断点
    /// </summary>
    public void ClearBreakpoints()
    {
        _breakpoints.Clear();
        _breakpointLookup.Clear();
    }

    #endregion

    #region 单步执行

    /// <summary>
    /// 单步跳过
    /// </summary>
    public void StepOver()
    {
        _stepMode = StepMode.StepOver;
        _stepDepth = _currentDepth;
        _isPaused = false;
    }

    /// <summary>
    /// 单步进入
    /// </summary>
    public void StepInto()
    {
        _stepMode = StepMode.StepInto;
        _isPaused = false;
    }

    /// <summary>
    /// 单步跳出
    /// </summary>
    public void StepOut()
    {
        _stepMode = StepMode.StepOut;
        _stepDepth = _currentDepth - 1;
        _isPaused = false;
    }

    /// <summary>
    /// 继续执行
    /// </summary>
    public void Continue()
    {
        _stepMode = StepMode.None;
        _isPaused = false;
    }

    /// <summary>
    /// 暂停执行
    /// </summary>
    public void Pause()
    {
        _isPaused = true;
        var snapshot = _vmState.CreateSnapshot();
        DebugEvent?.Invoke(this, new DebugEventArgs(DebugEventType.PauseRequested, snapshot));
    }

    #endregion

    #region 变量监视

    /// <summary>
    /// 添加变量监视
    /// </summary>
    public VariableWatch AddWatch(string name, VariableScope scope, int index)
    {
        var watch = new VariableWatch(name, scope, index);
        _watches.Add(watch);
        return watch;
    }

    /// <summary>
    /// 移除变量监视
    /// </summary>
    public void RemoveWatch(string name)
    {
        _watches.RemoveAll(w => w.Name == name);
    }

    /// <summary>
    /// 获取所有监视变量的当前值
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetWatchValues()
    {
        var result = new Dictionary<string, object?>();

        foreach (var watch in _watches)
        {
            result[watch.Name] = ResolveWatchValue(watch);
        }

        return result;
    }

    /// <summary>
    /// 获取当前调用栈的局部变量
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetLocalVariables()
    {
        var result = new Dictionary<string, object?>();
        var frames = _vmState.CallFrames;

        if (frames.Count == 0)
        {
            return result;
        }

        var currentFrame = frames[^1];

        for (var i = 0; i < currentFrame.Locals.Length; i++)
        {
            result[$"local_{i}"] = currentFrame.Locals[i];
        }

        return result;
    }

    /// <summary>
    /// 获取全局变量
    /// </summary>
    public IReadOnlyDictionary<int, object?> GetGlobalVariables()
    {
        var result = new Dictionary<int, object?>();

        for (var i = 0; i < _vmState.GlobalCount; i++)
        {
            var value = _vmState.GetGlobal(i);

            if (value is not null)
            {
                result[i] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// 获取操作数栈内容
    /// </summary>
    public object?[] GetStackContents()
    {
        return _vmState.Stack;
    }

    #endregion

    #region 指令执行回调

    /// <summary>
    /// 在每条指令执行前调用，检查断点和单步
    /// </summary>
    /// <returns>是否应暂停执行</returns>
    public bool OnBeforeExecute()
    {
        if (!_isAttached)
        {
            return false;
        }

        _currentDepth = _vmState.CallFrames.Count;

        if (CheckBreakpointHit())
        {
            _isPaused = true;
            var snapshot = _vmState.CreateSnapshot();
            var bp = FindBreakpointAtCurrentIP();
            DebugEvent?.Invoke(this, new DebugEventArgs(DebugEventType.BreakpointHit, snapshot, bp));
            return true;
        }

        if (CheckStepComplete())
        {
            _isPaused = true;
            _stepMode = StepMode.None;
            var snapshot = _vmState.CreateSnapshot();
            DebugEvent?.Invoke(this, new DebugEventArgs(DebugEventType.StepComplete, snapshot));
            return true;
        }

        return _isPaused;
    }

    /// <summary>
    /// 在异常抛出时调用
    /// </summary>
    public void OnException(VM.VMException exception)
    {
        if (!_isAttached)
        {
            return;
        }

        _isPaused = true;
        var snapshot = _vmState.CreateSnapshot();
        DebugEvent?.Invoke(this, new DebugEventArgs(DebugEventType.ExceptionThrown, snapshot, message: exception.Message));
    }

    #endregion

    #region 内部方法

    private bool CheckBreakpointHit()
    {
        var moduleName = _vmState.CurrentModule?.Name;

        if (moduleName is null)
        {
            return false;
        }

        var key = (moduleName, _vmState.IP);

        if (!_breakpointLookup.TryGetValue(key, out var bp))
        {
            return false;
        }

        return bp.Hit();
    }

    private bool CheckStepComplete()
    {
        return _stepMode switch
        {
            StepMode.StepInto => true,
            StepMode.StepOver => _currentDepth <= _stepDepth,
            StepMode.StepOut => _currentDepth <= _stepDepth,
            _ => false
        };
    }

    private Breakpoint? FindBreakpointAtCurrentIP()
    {
        var moduleName = _vmState.CurrentModule?.Name;

        if (moduleName is null)
        {
            return null;
        }

        _breakpointLookup.TryGetValue((moduleName, _vmState.IP), out var bp);
        return bp;
    }

    private object? ResolveWatchValue(VariableWatch watch)
    {
        return watch.Scope switch
        {
            VariableScope.Local => ResolveLocal(watch.Index),
            VariableScope.Global => _vmState.GetGlobal(watch.Index),
            VariableScope.Stack => ResolveStack(watch.Index),
            _ => null
        };
    }

    private object? ResolveLocal(int index)
    {
        var frames = _vmState.CallFrames;

        if (frames.Count == 0)
        {
            return null;
        }

        var currentFrame = frames[^1];
        return index >= 0 && index < currentFrame.Locals.Length ? currentFrame.Locals[index] : null;
    }

    private static object? ResolveStack(int index)
    {
        return index >= 0 ? null : null;
    }

    #endregion
}

/// <summary>
/// 变量监视项
/// </summary>
public sealed class VariableWatch
{
    #region Properties

    public string Name { get; }
    public VariableScope Scope { get; }
    public int Index { get; }

    #endregion

    #region Constructors

    public VariableWatch(string name, VariableScope scope, int index)
    {
        Name = name;
        Scope = scope;
        Index = index;
    }

    #endregion
}

/// <summary>
/// 变量作用域
/// </summary>
public enum VariableScope
{
    Local,
    Global,
    Stack
}

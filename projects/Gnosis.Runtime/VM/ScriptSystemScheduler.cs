using System.Collections.Concurrent;
using System.Diagnostics;
using Gnosis.ECS.System;

namespace Gnosis.Runtime.VM;

/// <summary>
/// 脚本系统信息，由 DefineSystem 指令创建。
/// 记录脚本定义的系统名称、执行阶段和字节码中的函数入口地址。
/// </summary>
public sealed class ScriptSystemInfo
{
    /// <summary>
    /// 系统名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 系统执行阶段
    /// </summary>
    public SystemPhase Phase { get; }

    /// <summary>
    /// 字节码中的函数入口地址
    /// </summary>
    public int FunctionAddress { get; }

    /// <summary>
    /// 系统索引（在调度器中的注册序号）
    /// </summary>
    public int SystemIndex { get; }

    /// <summary>
    /// 系统是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    public ScriptSystemInfo(string name, SystemPhase phase, int functionAddress, int systemIndex)
    {
        Name = name;
        Phase = phase;
        FunctionAddress = functionAddress;
        SystemIndex = systemIndex;
    }
}

/// <summary>
/// 脚本系统调度器，管理 GGScript 定义的系统执行顺序。
/// 支持按 SystemPhase 分阶段调度、依赖声明、拓扑排序缓存、并行执行和帧预算自适应调度。
/// </summary>
public sealed class ScriptSystemScheduler
{
    #region 字段

    private readonly List<ScriptSystemInfo> _systems = new();
    private readonly Dictionary<int, HashSet<int>> _dependencies = new();
    private readonly Dictionary<SystemPhase, List<int>> _phaseOrder = new();
    private readonly Dictionary<SystemPhase, List<List<int>>> _parallelBatches = new();
    private readonly HashSet<int> _disabledSystems = new();
    private bool _needsResort;

    private readonly Stopwatch _frameStopwatch = new();
    private double _frameBudgetMs = 16.0;
    private readonly Dictionary<int, double> _systemAvgTimeMs = new();
    private int _frameCount;

    #endregion

    #region 属性

    /// <summary>
    /// 已注册的脚本系统数量
    /// </summary>
    public int SystemCount => _systems.Count;

    /// <summary>
    /// 所有已注册的脚本系统
    /// </summary>
    public IReadOnlyList<ScriptSystemInfo> Systems => _systems;

    /// <summary>
    /// 是否启用并行执行
    /// </summary>
    public bool EnableParallelExecution { get; set; }

    /// <summary>
    /// 帧预算（毫秒），默认 16ms（60fps）
    /// </summary>
    public double FrameBudgetMs
    {
        get => _frameBudgetMs;
        set => _frameBudgetMs = Math.Max(0.1, value);
    }

    /// <summary>
    /// 上一帧的实际执行时间（毫秒）
    /// </summary>
    public double LastFrameTimeMs { get; private set; }

    /// <summary>
    /// 是否在上一帧中因帧预算不足而跳过了低优先级系统
    /// </summary>
    public bool LastFrameBudgetExceeded { get; private set; }

    #endregion

    #region 构造函数

    public ScriptSystemScheduler()
    {
        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            _phaseOrder[phase] = new List<int>();
            _parallelBatches[phase] = new List<List<int>>();
        }

        _needsResort = false;
        EnableParallelExecution = true;
    }

    #endregion

    #region 系统注册

    /// <summary>
    /// 注册脚本系统，返回系统索引
    /// </summary>
    public int RegisterSystem(ScriptSystemInfo system)
    {
        var idx = system.SystemIndex;

        while (_systems.Count <= idx)
        {
            _systems.Add(null!);
        }

        _systems[idx] = system;
        _dependencies[idx] = new HashSet<int>();
        _phaseOrder[system.Phase].Add(idx);
        _needsResort = true;

        return idx;
    }

    /// <summary>
    /// 根据索引获取脚本系统
    /// </summary>
    public ScriptSystemInfo? GetSystem(int systemIdx)
    {
        if (systemIdx < 0 || systemIdx >= _systems.Count)
        {
            return null;
        }

        return _systems[systemIdx];
    }

    #endregion

    #region 依赖管理

    /// <summary>
    /// 添加系统间依赖关系（被依赖者先执行）
    /// </summary>
    public void AddDependency(int systemIdx, int dependsOnIdx)
    {
        if (!_dependencies.ContainsKey(systemIdx))
        {
            _dependencies[systemIdx] = new HashSet<int>();
        }

        _dependencies[systemIdx].Add(dependsOnIdx);
        _needsResort = true;
    }

    /// <summary>
    /// 获取指定系统的依赖列表
    /// </summary>
    public IReadOnlySet<int> GetDependencies(int systemIdx)
    {
        return _dependencies.TryGetValue(systemIdx, out var deps) ? deps : new HashSet<int>();
    }

    #endregion

    #region 调度执行

    /// <summary>
    /// 获取按阶段和依赖排序后的系统执行顺序。
    /// 同一阶段内按拓扑排序确定执行顺序，无依赖的系统按注册顺序执行。
    /// </summary>
    public List<ScriptSystemInfo> GetExecutionOrder()
    {
        if (_needsResort)
        {
            Resort();
            _needsResort = false;
        }

        var result = new List<ScriptSystemInfo>();

        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            var sorted = _phaseOrder[phase];
            foreach (var idx in sorted)
            {
                if (idx < _systems.Count && _systems[idx] is not null && _systems[idx].Enabled && !_disabledSystems.Contains(idx))
                {
                    result.Add(_systems[idx]);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 获取指定阶段的系统执行顺序
    /// </summary>
    public List<ScriptSystemInfo> GetPhaseExecutionOrder(SystemPhase phase)
    {
        if (_needsResort)
        {
            Resort();
            _needsResort = false;
        }

        var result = new List<ScriptSystemInfo>();

        foreach (var idx in _phaseOrder[phase])
        {
            if (idx < _systems.Count && _systems[idx] is not null && _systems[idx].Enabled && !_disabledSystems.Contains(idx))
            {
                result.Add(_systems[idx]);
            }
        }

        return result;
    }

    /// <summary>
    /// 执行一帧的所有系统，支持并行分派和帧预算自适应调度。
    /// 返回实际执行的系统列表。
    /// </summary>
    /// <param name="executeSystem">系统执行回调，接收系统索引</param>
    /// <returns>实际执行的系统索引列表</returns>
    public List<int> ExecuteFrame(Action<int> executeSystem)
    {
        if (_needsResort)
        {
            Resort();
            _needsResort = false;
        }

        _frameStopwatch.Restart();
        var executed = new List<int>();
        var budgetExceeded = false;

        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            if (EnableParallelExecution && _parallelBatches[phase].Count > 0)
            {
                ExecutePhaseParallel(phase, executeSystem, executed, ref budgetExceeded);
            }
            else
            {
                ExecutePhaseSequential(phase, executeSystem, executed, ref budgetExceeded);
            }

            if (budgetExceeded) break;
        }

        LastFrameTimeMs = _frameStopwatch.Elapsed.TotalMilliseconds;
        LastFrameBudgetExceeded = budgetExceeded;
        _frameCount++;

        return executed;
    }

    #endregion

    #region 系统状态管理

    /// <summary>
    /// 启用指定系统
    /// </summary>
    public void EnableSystem(int systemIdx)
    {
        _disabledSystems.Remove(systemIdx);

        if (systemIdx >= 0 && systemIdx < _systems.Count && _systems[systemIdx] is not null)
        {
            _systems[systemIdx].Enabled = true;
        }
    }

    /// <summary>
    /// 禁用指定系统
    /// </summary>
    public void DisableSystem(int systemIdx)
    {
        _disabledSystems.Add(systemIdx);

        if (systemIdx >= 0 && systemIdx < _systems.Count && _systems[systemIdx] is not null)
        {
            _systems[systemIdx].Enabled = false;
        }
    }

    /// <summary>
    /// 检查系统是否被禁用
    /// </summary>
    public bool IsDisabled(int systemIdx)
    {
        return _disabledSystems.Contains(systemIdx);
    }

    #endregion

    #region 性能统计

    /// <summary>
    /// 获取指定系统的平均执行时间（毫秒）
    /// </summary>
    public double GetSystemAvgTimeMs(int systemIdx)
    {
        return _systemAvgTimeMs.TryGetValue(systemIdx, out var time) ? time : 0;
    }

    /// <summary>
    /// 记录系统执行时间（由外部调用）
    /// </summary>
    public void RecordSystemTime(int systemIdx, double elapsedMs)
    {
        if (!_systemAvgTimeMs.TryGetValue(systemIdx, out var avg))
        {
            _systemAvgTimeMs[systemIdx] = elapsedMs;
        }
        else
        {
            _systemAvgTimeMs[systemIdx] = avg * 0.9 + elapsedMs * 0.1;
        }
    }

    #endregion

    #region 私有方法

    private void Resort()
    {
        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            _phaseOrder[phase] = TopologicalSort(_phaseOrder[phase]);
            _parallelBatches[phase] = BuildParallelBatches(_phaseOrder[phase]);
        }
    }

    private List<int> TopologicalSort(List<int> systemIndices)
    {
        if (systemIndices.Count <= 1)
        {
            return systemIndices;
        }

        var indexSet = new HashSet<int>(systemIndices);
        var inDegree = new Dictionary<int, int>();
        var adj = new Dictionary<int, List<int>>();

        foreach (var idx in systemIndices)
        {
            inDegree[idx] = 0;
            adj[idx] = new List<int>();
        }

        foreach (var idx in systemIndices)
        {
            if (_dependencies.TryGetValue(idx, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (indexSet.Contains(dep))
                    {
                        adj[dep].Add(idx);
                        inDegree[idx]++;
                    }
                }
            }
        }

        var queue = new Queue<int>();
        foreach (var kvp in inDegree)
        {
            if (kvp.Value == 0)
            {
                queue.Enqueue(kvp.Key);
            }
        }

        var result = new List<int>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            foreach (var next in adj[current])
            {
                inDegree[next]--;
                if (inDegree[next] == 0)
                {
                    queue.Enqueue(next);
                }
            }
        }

        foreach (var idx in systemIndices)
        {
            if (!result.Contains(idx))
            {
                result.Add(idx);
            }
        }

        return result;
    }

    private List<List<int>> BuildParallelBatches(List<int> sortedIndices)
    {
        var batches = new List<List<int>>();

        if (sortedIndices.Count == 0)
        {
            return batches;
        }

        var remaining = new HashSet<int>(sortedIndices);
        var executed = new HashSet<int>();

        while (remaining.Count > 0)
        {
            var batch = new List<int>();

            foreach (var idx in remaining)
            {
                var deps = _dependencies.TryGetValue(idx, out var d) ? d : new HashSet<int>();
                bool allDepsExecuted = true;

                foreach (var dep in deps)
                {
                    if (!executed.Contains(dep))
                    {
                        allDepsExecuted = false;
                        break;
                    }
                }

                if (allDepsExecuted)
                {
                    batch.Add(idx);
                }
            }

            if (batch.Count == 0)
            {
                batches.Add(new List<int>(remaining));
                break;
            }

            foreach (var idx in batch)
            {
                remaining.Remove(idx);
                executed.Add(idx);
            }

            batches.Add(batch);
        }

        return batches;
    }

    private void ExecutePhaseSequential(SystemPhase phase, Action<int> executeSystem,
        List<int> executed, ref bool budgetExceeded)
    {
        foreach (var idx in _phaseOrder[phase])
        {
            if (idx >= _systems.Count || _systems[idx] is null || !_systems[idx].Enabled || _disabledSystems.Contains(idx))
            {
                continue;
            }

            if (budgetExceeded)
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            executeSystem(idx);
            sw.Stop();

            RecordSystemTime(idx, sw.Elapsed.TotalMilliseconds);
            executed.Add(idx);

            if (_frameStopwatch.Elapsed.TotalMilliseconds > _frameBudgetMs)
            {
                budgetExceeded = true;
            }
        }
    }

    private void ExecutePhaseParallel(SystemPhase phase, Action<int> executeSystem,
        List<int> executed, ref bool budgetExceeded)
    {
        foreach (var batch in _parallelBatches[phase])
        {
            if (budgetExceeded)
            {
                return;
            }

            if (batch.Count == 1)
            {
                var idx = batch[0];
                if (idx < _systems.Count && _systems[idx] is not null && _systems[idx].Enabled && !_disabledSystems.Contains(idx))
                {
                    var sw = Stopwatch.StartNew();
                    executeSystem(idx);
                    sw.Stop();

                    RecordSystemTime(idx, sw.Elapsed.TotalMilliseconds);
                    executed.Add(idx);
                }
            }
            else
            {
                var exceptions = new ConcurrentBag<Exception>();
                var batchExecuted = new ConcurrentBag<int>();
                var batchTimes = new ConcurrentDictionary<int, double>();

                Parallel.ForEach(batch, idx =>
                {
                    if (idx >= _systems.Count || _systems[idx] is null || !_systems[idx].Enabled || _disabledSystems.Contains(idx))
                    {
                        return;
                    }

                    try
                    {
                        var sw = Stopwatch.StartNew();
                        executeSystem(idx);
                        sw.Stop();

                        batchTimes[idx] = sw.Elapsed.TotalMilliseconds;
                        batchExecuted.Add(idx);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });

                if (!exceptions.IsEmpty)
                {
                    throw new AggregateException("并行脚本系统执行出错", exceptions);
                }

                foreach (var idx in batchExecuted)
                {
                    executed.Add(idx);
                    RecordSystemTime(idx, batchTimes[idx]);
                }
            }

            if (_frameStopwatch.Elapsed.TotalMilliseconds > _frameBudgetMs)
            {
                budgetExceeded = true;
            }
        }
    }

    #endregion
}

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
/// 支持按 SystemPhase 分阶段调度、依赖声明和拓扑排序。
/// 当前阶段实现串行调度，远期支持并行执行。
/// </summary>
public sealed class ScriptSystemScheduler
{
    #region 字段

    private readonly List<ScriptSystemInfo> _systems = new();
    private readonly Dictionary<int, HashSet<int>> _dependencies = new();
    private readonly Dictionary<SystemPhase, List<int>> _phaseOrder = new();
    private bool _needsResort;

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

    #endregion

    #region 构造函数

    public ScriptSystemScheduler()
    {
        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            _phaseOrder[phase] = new List<int>();
        }

        _needsResort = false;
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
                if (idx < _systems.Count && _systems[idx] is not null && _systems[idx].Enabled)
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
            if (idx < _systems.Count && _systems[idx] is not null && _systems[idx].Enabled)
            {
                result.Add(_systems[idx]);
            }
        }

        return result;
    }

    #endregion

    #region 系统状态管理

    /// <summary>
    /// 启用指定系统
    /// </summary>
    public void EnableSystem(int systemIdx)
    {
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
        if (systemIdx >= 0 && systemIdx < _systems.Count && _systems[systemIdx] is not null)
        {
            _systems[systemIdx].Enabled = false;
        }
    }

    #endregion

    #region 私有方法

    private void Resort()
    {
        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            _phaseOrder[phase] = TopologicalSort(_phaseOrder[phase]);
        }
    }

    /// <summary>
    /// 对指定阶段内的系统进行拓扑排序，确保依赖系统先执行
    /// </summary>
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

    #endregion
}

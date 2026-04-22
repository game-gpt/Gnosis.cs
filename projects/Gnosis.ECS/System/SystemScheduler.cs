using System.Collections.Concurrent;

namespace Gnosis.ECS.System;

/// <summary>
/// 系统调度器，基于依赖图和系统阶段进行调度。
/// 支持 World 注入和同一阶段内无依赖系统的并行执行。
/// </summary>
public sealed class SystemScheduler : ISystemScheduler
{
    #region 字段

    private readonly Dictionary<SystemPhase, DependencyGraph> _phaseGraphs = new();
    private readonly Dictionary<SystemPhase, List<ISystem>> _sortedSystems = new();
    private readonly Dictionary<SystemPhase, List<List<ISystem>>> _parallelBatches = new();
    private readonly HashSet<ISystem> _disabledSystems = new();
    private readonly HashSet<ISystem> _allSystems = new();
    private bool _needsResort;
    private World.World? _world;

    #endregion

    #region 属性

    /// <summary>
    /// 已注册的系统总数
    /// </summary>
    public int SystemCount => _allSystems.Count;

    /// <summary>
    /// 是否启用并行执行
    /// </summary>
    public bool EnableParallelExecution { get; set; }

    #endregion

    #region 构造函数

    public SystemScheduler()
    {
        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            _phaseGraphs[phase] = new DependencyGraph();
            _sortedSystems[phase] = [];
            _parallelBatches[phase] = [];
        }

        _needsResort = false;
        EnableParallelExecution = true;
    }

    #endregion

    #region 公开方法 - 系统注册

    /// <summary>
    /// 注册系统，自动注入 World（如果系统实现 IWorldSystem）
    /// </summary>
    public void RegisterSystem(ISystem system)
    {
        if (_allSystems.Contains(system)) return;

        if (system is IWorldSystem worldSystem && _world != null)
        {
            worldSystem.SetWorld(_world);
        }

        _allSystems.Add(system);
        _phaseGraphs[system.Phase].AddSystem(system);
        _needsResort = true;

        system.Initialize();
    }

    /// <summary>
    /// 注销系统
    /// </summary>
    public void UnregisterSystem(ISystem system)
    {
        if (!_allSystems.Contains(system)) return;

        _allSystems.Remove(system);
        _disabledSystems.Remove(system);
        _phaseGraphs[system.Phase].RemoveSystem(system);
        _needsResort = true;

        system.Shutdown();
    }

    #endregion

    #region 公开方法 - 调度执行

    /// <summary>
    /// 更新所有活跃系统。
    /// 同一阶段内，无依赖的系统将并行执行（如果 EnableParallelExecution 为 true）。
    /// </summary>
    public void Update(float delta)
    {
        if (_needsResort)
        {
            ResortSystems();
            _needsResort = false;
        }

        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            if (EnableParallelExecution && _parallelBatches[phase].Count > 0)
            {
                ExecutePhaseParallel(phase, delta);
            }
            else
            {
                ExecutePhaseSequential(phase, delta);
            }
        }
    }

    #endregion

    #region 公开方法 - 系统状态管理

    /// <summary>
    /// 启用指定系统
    /// </summary>
    public void EnableSystem(ISystem system)
    {
        _disabledSystems.Remove(system);
    }

    /// <summary>
    /// 禁用指定系统
    /// </summary>
    public void DisableSystem(ISystem system)
    {
        _disabledSystems.Add(system);
    }

    /// <summary>
    /// 检查系统是否被禁用
    /// </summary>
    public bool IsDisabled(ISystem system)
    {
        return _disabledSystems.Contains(system);
    }

    #endregion

    #region 公开方法 - 依赖管理

    /// <summary>
    /// 添加系统间依赖关系（被依赖者先执行）
    /// </summary>
    public void AddDependency(ISystem system, ISystem dependsOn)
    {
        if (system.Phase != dependsOn.Phase) return;

        _phaseGraphs[system.Phase].AddDependency(system, dependsOn);
        _needsResort = true;
    }

    #endregion

    #region 公开方法 - World 绑定

    /// <summary>
    /// 设置调度器所属的 World，并注入到所有已注册的 IWorldSystem
    /// </summary>
    public void SetWorld(World.World world)
    {
        _world = world;

        foreach (var system in _allSystems)
        {
            if (system is IWorldSystem worldSystem)
            {
                worldSystem.SetWorld(world);
            }
        }
    }

    #endregion

    #region 私有方法 - 阶段执行

    private void ExecutePhaseSequential(SystemPhase phase, float delta)
    {
        foreach (var system in _sortedSystems[phase])
        {
            if (_disabledSystems.Contains(system)) continue;

            system.Update(delta);
        }
    }

    private void ExecutePhaseParallel(SystemPhase phase, float delta)
    {
        foreach (var batch in _parallelBatches[phase])
        {
            if (batch.Count == 1)
            {
                var system = batch[0];

                if (!_disabledSystems.Contains(system))
                {
                    system.Update(delta);
                }
            }
            else
            {
                ExecuteBatchParallel(batch, delta);
            }
        }
    }

    private void ExecuteBatchParallel(List<ISystem> batch, float delta)
    {
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.ForEach(batch, system =>
        {
            if (_disabledSystems.Contains(system)) return;

            try
            {
                system.Update(delta);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        if (!exceptions.IsEmpty)
        {
            throw new AggregateException("并行系统执行出错", exceptions);
        }
    }

    #endregion

    #region 私有方法 - 重新排序

    private void ResortSystems()
    {
        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            _sortedSystems[phase] = _phaseGraphs[phase].TopologicalSort();
            _parallelBatches[phase] = BuildParallelBatches(_sortedSystems[phase], _phaseGraphs[phase]);
        }
    }

    /// <summary>
    /// 将拓扑排序后的系统列表分组为可并行执行的批次。
    /// 同一批次内的系统之间没有依赖关系。
    /// </summary>
    private static List<List<ISystem>> BuildParallelBatches(List<ISystem> sortedSystems, DependencyGraph graph)
    {
        var batches = new List<List<ISystem>>();

        if (sortedSystems.Count == 0)
        {
            return batches;
        }

        var remaining = new HashSet<ISystem>(sortedSystems);
        var executed = new HashSet<ISystem>();

        while (remaining.Count > 0)
        {
            var batch = new List<ISystem>();

            foreach (var system in remaining)
            {
                var dependencies = graph.GetDependencies(system);
                bool allDependenciesExecuted = true;

                foreach (var dep in dependencies)
                {
                    if (!executed.Contains(dep))
                    {
                        allDependenciesExecuted = false;
                        break;
                    }
                }

                if (allDependenciesExecuted)
                {
                    batch.Add(system);
                }
            }

            if (batch.Count == 0)
            {
                // 存在循环依赖，回退到顺序执行
                batches.Add(new List<ISystem>(remaining));
                break;
            }

            foreach (var system in batch)
            {
                remaining.Remove(system);
                executed.Add(system);
            }

            batches.Add(batch);
        }

        return batches;
    }

    #endregion
}

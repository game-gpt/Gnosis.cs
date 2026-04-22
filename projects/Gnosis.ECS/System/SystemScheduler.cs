namespace Gnosis.ECS.System;

/// <summary>
/// 系统调度器，基于依赖图和系统阶段进行调度
/// </summary>
public sealed class SystemScheduler : ISystemScheduler
{
    #region 字段

    private readonly Dictionary<SystemPhase, DependencyGraph> _phaseGraphs = new();
    private readonly Dictionary<SystemPhase, List<ISystem>> _sortedSystems = new();
    private readonly HashSet<ISystem> _disabledSystems = new();
    private readonly HashSet<ISystem> _allSystems = new();
    private bool _needsResort;

    #endregion

    #region 属性

    public int SystemCount => _allSystems.Count;

    #endregion

    #region 构造函数

    public SystemScheduler()
    {
        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            _phaseGraphs[phase] = new DependencyGraph();
            _sortedSystems[phase] = [];
        }

        _needsResort = false;
    }

    #endregion

    #region 公开方法

    public void RegisterSystem(ISystem system)
    {
        if (_allSystems.Contains(system)) return;

        _allSystems.Add(system);
        _phaseGraphs[system.Phase].AddSystem(system);
        _needsResort = true;

        system.Initialize();
    }

    public void UnregisterSystem(ISystem system)
    {
        if (!_allSystems.Contains(system)) return;

        _allSystems.Remove(system);
        _disabledSystems.Remove(system);
        _phaseGraphs[system.Phase].RemoveSystem(system);
        _needsResort = true;

        system.Shutdown();
    }

    public void Update(float delta)
    {
        if (_needsResort)
        {
            ResortSystems();
            _needsResort = false;
        }

        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            foreach (var system in _sortedSystems[phase])
            {
                if (_disabledSystems.Contains(system)) continue;

                system.Update(delta);
            }
        }
    }

    public void EnableSystem(ISystem system)
    {
        _disabledSystems.Remove(system);
    }

    public void DisableSystem(ISystem system)
    {
        _disabledSystems.Add(system);
    }

    public void AddDependency(ISystem system, ISystem dependsOn)
    {
        if (system.Phase != dependsOn.Phase) return;

        _phaseGraphs[system.Phase].AddDependency(system, dependsOn);
        _needsResort = true;
    }

    #endregion

    #region 私有方法

    private void ResortSystems()
    {
        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            _sortedSystems[phase] = _phaseGraphs[phase].TopologicalSort();
        }
    }

    #endregion
}

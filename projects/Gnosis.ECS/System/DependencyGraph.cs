namespace Gnosis.ECS.System;

/// <summary>
/// 系统间依赖图，构建系统执行顺序的拓扑排序
/// </summary>
public sealed class DependencyGraph
{
    #region 字段

    private readonly Dictionary<ISystem, HashSet<ISystem>> _dependencies = new();
    private readonly Dictionary<ISystem, HashSet<ISystem>> _dependents = new();

    #endregion

    #region 属性

    public int SystemCount => _dependencies.Count;

    #endregion

    #region 公开方法

    public void AddSystem(ISystem system)
    {
        if (!_dependencies.ContainsKey(system))
        {
            _dependencies[system] = [];
        }

        if (!_dependents.ContainsKey(system))
        {
            _dependents[system] = [];
        }
    }

    public void AddDependency(ISystem system, ISystem dependsOn)
    {
        AddSystem(system);
        AddSystem(dependsOn);

        _dependencies[system].Add(dependsOn);
        _dependents[dependsOn].Add(system);
    }

    public void RemoveSystem(ISystem system)
    {
        if (_dependencies.TryGetValue(system, out var deps))
        {
            foreach (var dep in deps)
            {
                if (_dependents.TryGetValue(dep, out var dependents))
                {
                    dependents.Remove(system);
                }
            }
        }

        if (_dependents.TryGetValue(system, out var dependents2))
        {
            foreach (var dependent in dependents2)
            {
                if (_dependencies.TryGetValue(dependent, out var deps2))
                {
                    deps2.Remove(system);
                }
            }
        }

        _dependencies.Remove(system);
        _dependents.Remove(system);
    }

    public List<ISystem> TopologicalSort()
    {
        var result = new List<ISystem>();
        var inDegree = new Dictionary<ISystem, int>();

        foreach (var system in _dependencies.Keys)
        {
            inDegree[system] = 0;
        }

        foreach (var kvp in _dependencies)
        {
            foreach (var dep in kvp.Value)
            {
                inDegree[kvp.Key]++;
            }
        }

        var queue = new Queue<ISystem>();

        foreach (var kvp in inDegree)
        {
            if (kvp.Value == 0)
            {
                queue.Enqueue(kvp.Key);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            if (_dependents.TryGetValue(current, out var dependents))
            {
                foreach (var dependent in dependents)
                {
                    inDegree[dependent]--;

                    if (inDegree[dependent] == 0)
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }
        }

        return result;
    }

    public IReadOnlySet<ISystem> GetDependencies(ISystem system)
    {
        return _dependencies.TryGetValue(system, out var deps) ? deps : new HashSet<ISystem>();
    }

    public IReadOnlySet<ISystem> GetDependents(ISystem system)
    {
        return _dependents.TryGetValue(system, out var deps) ? deps : new HashSet<ISystem>();
    }

    #endregion
}

using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Dependency;

/// <summary>
/// 插件依赖图解析器，负责拓扑排序与循环依赖检测
/// </summary>
public sealed class DependencyResolver
{
    #region 字段

    private readonly Dictionary<string, PluginManifest> _registeredManifests;
    private readonly Dictionary<string, List<PluginDependency>> _dependencyEdges;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化依赖解析器
    /// </summary>
    public DependencyResolver()
    {
        _registeredManifests = new Dictionary<string, PluginManifest>(StringComparer.OrdinalIgnoreCase);
        _dependencyEdges = new Dictionary<string, List<PluginDependency>>(StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 注册插件清单到解析器
    /// </summary>
    /// <param name="manifest">插件清单</param>
    public void Register(PluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        _registeredManifests[manifest.Id] = manifest;
        _dependencyEdges[manifest.Id] = new List<PluginDependency>(manifest.Dependencies);
    }

    /// <summary>
    /// 批量注册插件清单
    /// </summary>
    /// <param name="manifests">插件清单集合</param>
    public void RegisterRange(IEnumerable<PluginManifest> manifests)
    {
        ArgumentNullException.ThrowIfNull(manifests);

        foreach (var manifest in manifests)
        {
            Register(manifest);
        }
    }

    /// <summary>
    /// 移除已注册的插件清单
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    public void Unregister(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
        {
            return;
        }

        _registeredManifests.Remove(pluginId);
        _dependencyEdges.Remove(pluginId);
    }

    /// <summary>
    /// 解析依赖图，返回拓扑排序后的加载顺序
    /// </summary>
    /// <returns>依赖解析结果</returns>
    public DependencyResolveResult Resolve()
    {
        var sorted = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cycles = new List<List<string>>();

        foreach (var pluginId in _registeredManifests.Keys)
        {
            if (!visited.Contains(pluginId))
            {
                Visit(pluginId, visited, visiting, sorted, cycles);
            }
        }

        if (cycles.Count > 0)
        {
            return DependencyResolveResult.Failure(
                cycles.Select(c => new DependencyCycle(c)).ToList(),
                null
            );
        }

        var missingDeps = FindMissingDependencies();

        if (missingDeps.Count > 0)
        {
            return DependencyResolveResult.Failure(
                null,
                missingDeps
            );
        }

        var versionConflicts = FindVersionConflicts();

        if (versionConflicts.Count > 0)
        {
            return DependencyResolveResult.Failure(
                null,
                null,
                versionConflicts
            );
        }

        return DependencyResolveResult.Success(sorted);
    }

    /// <summary>
    /// 检查指定插件的所有依赖是否已满足
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    /// <returns>是否所有依赖已满足</returns>
    public bool AreDependenciesSatisfied(string pluginId)
    {
        if (!_dependencyEdges.TryGetValue(pluginId, out var deps))
        {
            return true;
        }

        foreach (var dep in deps)
        {
            if (dep.IsOptional)
            {
                continue;
            }

            if (!_registeredManifests.ContainsKey(dep.PluginId))
            {
                return false;
            }

            var depManifest = _registeredManifests[dep.PluginId];

            if (!dep.IsSatisfiedBy(depManifest.Version))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 获取指定插件的直接依赖列表
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    /// <returns>依赖列表</returns>
    public IReadOnlyList<PluginDependency> GetDirectDependencies(string pluginId)
    {
        if (_dependencyEdges.TryGetValue(pluginId, out var deps))
        {
            return deps;
        }

        return Array.Empty<PluginDependency>();
    }

    /// <summary>
    /// 获取指定插件的所有传递依赖（递归）
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    /// <returns>传递依赖列表</returns>
    public IReadOnlyList<PluginDependency> GetTransitiveDependencies(string pluginId)
    {
        var result = new List<PluginDependency>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        CollectTransitiveDependencies(pluginId, result, visited);

        return result;
    }

    /// <summary>
    /// 获取依赖指定插件的所有插件（反向依赖）
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    /// <returns>反向依赖列表</returns>
    public IReadOnlyList<string> GetDependents(string pluginId)
    {
        var dependents = new List<string>();

        foreach (var (id, deps) in _dependencyEdges)
        {
            if (deps.Any(d => string.Equals(d.PluginId, pluginId, StringComparison.OrdinalIgnoreCase)))
            {
                dependents.Add(id);
            }
        }

        return dependents;
    }

    /// <summary>
    /// 清空所有已注册的插件清单
    /// </summary>
    public void Clear()
    {
        _registeredManifests.Clear();
        _dependencyEdges.Clear();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 深度优先遍历，进行拓扑排序和循环检测
    /// </summary>
    private void Visit(
        string pluginId,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<string> sorted,
        List<List<string>> cycles)
    {
        if (visiting.Contains(pluginId))
        {
            var cycleStart = sorted.Count > 0 ? sorted.LastIndexOf(pluginId) : -1;
            var cycle = cycleStart >= 0
                ? sorted.Skip(cycleStart).Append(pluginId).ToList()
                : new List<string> { pluginId };
            cycles.Add(cycle);
            return;
        }

        if (visited.Contains(pluginId))
        {
            return;
        }

        visiting.Add(pluginId);

        if (_dependencyEdges.TryGetValue(pluginId, out var deps))
        {
            foreach (var dep in deps)
            {
                if (!dep.IsOptional && _registeredManifests.ContainsKey(dep.PluginId))
                {
                    Visit(dep.PluginId, visited, visiting, sorted, cycles);
                }
            }
        }

        visiting.Remove(pluginId);
        visited.Add(pluginId);
        sorted.Add(pluginId);
    }

    /// <summary>
    /// 查找缺失的必需依赖
    /// </summary>
    private List<MissingDependency> FindMissingDependencies()
    {
        var missing = new List<MissingDependency>();

        foreach (var (pluginId, deps) in _dependencyEdges)
        {
            foreach (var dep in deps)
            {
                if (dep.IsOptional)
                {
                    continue;
                }

                if (!_registeredManifests.ContainsKey(dep.PluginId))
                {
                    missing.Add(new MissingDependency(pluginId, dep.PluginId, dep.VersionRange));
                }
            }
        }

        return missing;
    }

    /// <summary>
    /// 查找版本冲突
    /// </summary>
    private List<VersionConflict> FindVersionConflicts()
    {
        var conflicts = new List<VersionConflict>();

        foreach (var (pluginId, deps) in _dependencyEdges)
        {
            foreach (var dep in deps)
            {
                if (!_registeredManifests.TryGetValue(dep.PluginId, out var depManifest))
                {
                    continue;
                }

                if (!dep.IsSatisfiedBy(depManifest.Version))
                {
                    conflicts.Add(new VersionConflict(
                        pluginId,
                        dep.PluginId,
                        dep.VersionRange,
                        depManifest.Version
                    ));
                }
            }
        }

        return conflicts;
    }

    /// <summary>
    /// 递归收集传递依赖
    /// </summary>
    private void CollectTransitiveDependencies(
        string pluginId,
        List<PluginDependency> result,
        HashSet<string> visited)
    {
        if (!visited.Add(pluginId))
        {
            return;
        }

        if (!_dependencyEdges.TryGetValue(pluginId, out var deps))
        {
            return;
        }

        foreach (var dep in deps)
        {
            result.Add(dep);

            if (_registeredManifests.ContainsKey(dep.PluginId))
            {
                CollectTransitiveDependencies(dep.PluginId, result, visited);
            }
        }
    }

    #endregion
}

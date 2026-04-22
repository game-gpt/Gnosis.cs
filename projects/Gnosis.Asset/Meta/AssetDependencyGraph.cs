namespace Gnosis.Asset.Meta;

public sealed class AssetDependencyGraph
{
    private readonly Dictionary<string, HashSet<string>> _dependencies = new();
    private readonly Dictionary<string, HashSet<string>> _reverseDependencies = new();

    public int AssetCount => _dependencies.Count;

    public void AddAsset(string assetPath)
    {
        var normalized = NormalizePath(assetPath);
        if (!_dependencies.ContainsKey(normalized))
        {
            _dependencies[normalized] = new HashSet<string>();
        }
    }

    public void AddDependency(string assetPath, string dependsOn)
    {
        var normalizedAsset = NormalizePath(assetPath);
        var normalizedDep = NormalizePath(dependsOn);

        if (!_dependencies.ContainsKey(normalizedAsset))
        {
            _dependencies[normalizedAsset] = new HashSet<string>();
        }

        if (!_dependencies.ContainsKey(normalizedDep))
        {
            _dependencies[normalizedDep] = new HashSet<string>();
        }

        if (!_reverseDependencies.ContainsKey(normalizedDep))
        {
            _reverseDependencies[normalizedDep] = new HashSet<string>();
        }

        _dependencies[normalizedAsset].Add(normalizedDep);
        _reverseDependencies[normalizedDep].Add(normalizedAsset);
    }

    public bool RemoveAsset(string assetPath)
    {
        var normalized = NormalizePath(assetPath);

        if (!_dependencies.Remove(normalized))
        {
            return false;
        }

        foreach (var dep in _dependencies.Values)
        {
            dep.Remove(normalized);
        }

        _reverseDependencies.Remove(normalized);

        foreach (var reverse in _reverseDependencies.Values)
        {
            reverse.Remove(normalized);
        }

        return true;
    }

    public IReadOnlyList<string> GetDirectDependencies(string assetPath)
    {
        var normalized = NormalizePath(assetPath);
        return _dependencies.TryGetValue(normalized, out var deps)
            ? deps.ToList()
            : Array.Empty<string>();
    }

    public IReadOnlyList<string> GetTransitiveDependencies(string assetPath)
    {
        var normalized = NormalizePath(assetPath);
        var visited = new HashSet<string>();
        var stack = new Stack<string>();

        stack.Push(normalized);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!_dependencies.TryGetValue(current, out var deps))
            {
                continue;
            }

            foreach (var dep in deps)
            {
                if (visited.Add(dep))
                {
                    stack.Push(dep);
                }
            }
        }

        return visited.ToList();
    }

    public IReadOnlyList<string> GetReverseDependencies(string assetPath)
    {
        var normalized = NormalizePath(assetPath);
        return _reverseDependencies.TryGetValue(normalized, out var reverseDeps)
            ? reverseDeps.ToList()
            : Array.Empty<string>();
    }

    public bool HasCyclicDependency()
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var asset in _dependencies.Keys)
        {
            if (DfsDetectCycle(asset, visited, recursionStack))
            {
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<string> GetCyclePath()
    {
        var visited = new HashSet<string>();
        var recursionStack = new List<string>();

        foreach (var asset in _dependencies.Keys)
        {
            var cycle = DfsFindCycle(asset, visited, recursionStack);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        return Array.Empty<string>();
    }

    public IReadOnlyList<string> GetTopologicalOrder()
    {
        if (HasCyclicDependency())
        {
            throw new InvalidOperationException("依赖图中存在循环依赖，无法进行拓扑排序");
        }

        var inDegree = new Dictionary<string, int>();
        foreach (var asset in _dependencies.Keys)
        {
            inDegree[asset] = 0;
        }

        foreach (var deps in _dependencies.Values)
        {
            foreach (var dep in deps)
            {
                if (inDegree.ContainsKey(dep))
                {
                    inDegree[dep]++;
                }
            }
        }

        var queue = new Queue<string>();
        foreach (var kvp in inDegree)
        {
            if (kvp.Value == 0)
            {
                queue.Enqueue(kvp.Key);
            }
        }

        var result = new List<string>();
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            if (_dependencies.TryGetValue(current, out var deps))
            {
                foreach (var dep in deps)
                {
                    inDegree[dep]--;
                    if (inDegree[dep] == 0)
                    {
                        queue.Enqueue(dep);
                    }
                }
            }
        }

        return result;
    }

    public void Clear()
    {
        _dependencies.Clear();
        _reverseDependencies.Clear();
    }

    public bool ContainsAsset(string assetPath)
    {
        return _dependencies.ContainsKey(NormalizePath(assetPath));
    }

    private bool DfsDetectCycle(string node, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(node))
        {
            return true;
        }

        if (visited.Contains(node))
        {
            return false;
        }

        visited.Add(node);
        recursionStack.Add(node);

        if (_dependencies.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
            {
                if (DfsDetectCycle(dep, visited, recursionStack))
                {
                    return true;
                }
            }
        }

        recursionStack.Remove(node);
        return false;
    }

    private IReadOnlyList<string>? DfsFindCycle(string node, HashSet<string> visited, List<string> path)
    {
        if (path.Contains(node))
        {
            var cycleStart = path.IndexOf(node);
            var cycle = path.Skip(cycleStart).ToList();
            cycle.Add(node);
            return cycle;
        }

        if (visited.Contains(node))
        {
            return null;
        }

        visited.Add(node);
        path.Add(node);

        if (_dependencies.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
            {
                var cycle = DfsFindCycle(dep, visited, path);
                if (cycle is not null)
                {
                    return cycle;
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        return null;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}

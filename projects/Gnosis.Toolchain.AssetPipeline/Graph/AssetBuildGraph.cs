using Gnosis.Asset.Format;
using Gnosis.Asset.Meta;

namespace Gnosis.Toolchain.AssetPipeline.Graph;

public enum BuildNodeStatus
{
    Pending,
    Building,
    Succeeded,
    Failed,
    Skipped,
    UpToDate
}

public sealed record BuildNode
{
    public string AssetPath { get; init; } = string.Empty;
    public FormatType FormatType { get; init; }
    public AssetHash? SourceHash { get; set; }
    public AssetHash? OutputHash { get; set; }
    public string? OutputPath { get; set; }
    public BuildNodeStatus Status { get; set; } = BuildNodeStatus.Pending;
    public DateTime? LastBuildTime { get; set; }
    public IReadOnlyList<string> Dependencies { get; set; } = [];
    public string? ErrorMessage { get; set; }
    public TimeSpan BuildDuration { get; set; }
}

public sealed class AssetBuildGraph
{
    private readonly Dictionary<string, BuildNode> _nodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _dependencies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _reverseDependencies = new(StringComparer.OrdinalIgnoreCase);

    public int NodeCount => _nodes.Count;
    public int PendingCount => _nodes.Values.Count(n => n.Status == BuildNodeStatus.Pending);
    public int CompletedCount => _nodes.Values.Count(n => n.Status == BuildNodeStatus.Succeeded || n.Status == BuildNodeStatus.UpToDate);
    public int FailedCount => _nodes.Values.Count(n => n.Status == BuildNodeStatus.Failed);

    public void AddNode(BuildNode node)
    {
        if (string.IsNullOrWhiteSpace(node.AssetPath))
        {
            throw new ArgumentException("资产路径不能为空", nameof(node));
        }

        var normalizedPath = NormalizePath(node.AssetPath);
        var normalizedNode = node with { AssetPath = normalizedPath };

        _nodes[normalizedPath] = normalizedNode;

        if (!_dependencies.ContainsKey(normalizedPath))
        {
            _dependencies[normalizedPath] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public void AddDependency(string assetPath, string dependsOn)
    {
        var normalizedAsset = NormalizePath(assetPath);
        var normalizedDep = NormalizePath(dependsOn);

        if (!_dependencies.ContainsKey(normalizedAsset))
        {
            _dependencies[normalizedAsset] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        if (!_dependencies.ContainsKey(normalizedDep))
        {
            _dependencies[normalizedDep] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        if (!_reverseDependencies.ContainsKey(normalizedDep))
        {
            _reverseDependencies[normalizedDep] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        _dependencies[normalizedAsset].Add(normalizedDep);
        _reverseDependencies[normalizedDep].Add(normalizedAsset);

        if (_nodes.TryGetValue(normalizedAsset, out var node))
        {
            var deps = new List<string>(node.Dependencies) { normalizedDep };
            node.Dependencies = deps;
        }
    }

    public BuildNode? GetNode(string assetPath)
    {
        return _nodes.TryGetValue(NormalizePath(assetPath), out var node) ? node : null;
    }

    public bool RemoveNode(string assetPath)
    {
        var normalized = NormalizePath(assetPath);

        if (!_nodes.Remove(normalized))
        {
            return false;
        }

        _dependencies.Remove(normalized);

        foreach (var deps in _dependencies.Values)
        {
            deps.Remove(normalized);
        }

        _reverseDependencies.Remove(normalized);

        foreach (var reverse in _reverseDependencies.Values)
        {
            reverse.Remove(normalized);
        }

        return true;
    }

    public void UpdateNodeStatus(string assetPath, BuildNodeStatus status, string? outputPath = null, string? errorMessage = null)
    {
        var normalized = NormalizePath(assetPath);

        if (!_nodes.TryGetValue(normalized, out var node))
        {
            return;
        }

        node.Status = status;

        if (outputPath is not null)
        {
            node.OutputPath = outputPath;
        }

        if (errorMessage is not null)
        {
            node.ErrorMessage = errorMessage;
        }

        if (status is BuildNodeStatus.Succeeded or BuildNodeStatus.UpToDate)
        {
            node.LastBuildTime = DateTime.UtcNow;
        }
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
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

    public IReadOnlyList<string> GetTopologicalOrder()
    {
        var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

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
        foreach (var (path, degree) in inDegree)
        {
            if (degree == 0)
            {
                queue.Enqueue(path);
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

    public IReadOnlyList<string> GetBuildOrder()
    {
        var order = GetTopologicalOrder();
        return order.Where(p => _nodes.ContainsKey(p)).ToList();
    }

    public IReadOnlyList<string> GetOutdatedNodes()
    {
        return _nodes.Values
            .Where(n => n.Status is BuildNodeStatus.Pending or BuildNodeStatus.Failed)
            .Select(n => n.AssetPath)
            .ToList();
    }

    public IReadOnlyList<string> GetFailedNodes()
    {
        return _nodes.Values
            .Where(n => n.Status == BuildNodeStatus.Failed)
            .Select(n => n.AssetPath)
            .ToList();
    }

    public IReadOnlyList<BuildNode> GetAllNodes()
    {
        return _nodes.Values.ToList();
    }

    public void ResetAllStatus()
    {
        foreach (var node in _nodes.Values)
        {
            node.Status = BuildNodeStatus.Pending;
            node.ErrorMessage = null;
            node.BuildDuration = TimeSpan.Zero;
        }
    }

    public bool HasCyclicDependency()
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recursionStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new List<string>();

        foreach (var asset in _dependencies.Keys)
        {
            var cycle = DfsFindCycle(asset, visited, path);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        return Array.Empty<string>();
    }

    public void Clear()
    {
        _nodes.Clear();
        _dependencies.Clear();
        _reverseDependencies.Clear();
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
        return path.Replace('\\', '/').Trim('/');
    }
}

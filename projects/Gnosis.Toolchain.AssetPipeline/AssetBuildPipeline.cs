using Gnosis.Asset.Cache;
using Gnosis.Asset.Format;
using Gnosis.Asset.Meta;
using Gnosis.Toolchain.AssetPipeline.Bake;
using Gnosis.Toolchain.AssetPipeline.Graph;

namespace Gnosis.Toolchain.AssetPipeline;

public sealed class BuildPipelineOptions
{
    public string ContentRoot { get; init; } = string.Empty;
    public string OutputRoot { get; init; } = string.Empty;
    public string CacheDirectory { get; init; } = string.Empty;
    public bool EnableIncrementalBuild { get; init; } = true;
    public bool FailOnFirstError { get; init; } = false;
    public int MaxDegreeOfParallelism { get; init; } = 1;
}

public sealed class BuildPipelineResult
{
    public int TotalAssets { get; init; }
    public int SucceededCount { get; init; }
    public int FailedCount { get; init; }
    public int UpToDateCount { get; init; }
    public int SkippedCount { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public IReadOnlyList<BakeResult> Results { get; init; } = [];
    public IReadOnlyList<string> FailedAssets { get; init; } = [];

    public bool HasErrors => FailedCount > 0;
}

public sealed class AssetBuildPipeline : IDisposable
{
    private readonly AssetBuildGraph _graph = new();
    private readonly AssetBaker _baker;
    private readonly BuildPipelineOptions _options;
    private readonly BuildCache? _buildCache;
    private readonly IFormatRegistry _formatRegistry;
    private bool _disposed;

    public AssetBuildGraph Graph => _graph;
    public AssetBaker Baker => _baker;

    public AssetBuildPipeline(IFormatRegistry formatRegistry, BuildPipelineOptions options)
    {
        _formatRegistry = formatRegistry ?? throw new ArgumentNullException(nameof(formatRegistry));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (options.EnableIncrementalBuild && !string.IsNullOrWhiteSpace(options.CacheDirectory))
        {
            _buildCache = new BuildCache(options.CacheDirectory);
        }

        _baker = new AssetBaker(_formatRegistry, _buildCache);
    }

    public void DiscoverAssets(string? contentRoot = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var root = contentRoot ?? _options.ContentRoot;

        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException("内容根目录未指定");
        }

        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"内容根目录不存在：{root}");
        }

        var supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".ktx", ".tga", ".bmp", ".hdr",
            ".obj", ".gltf", ".glb", ".fbx",
            ".wav", ".mp3", ".ogg", ".flac",
            ".gnosis-anim", ".anim",
            ".gnosis-mat", ".mat",
            ".scene",
            ".prefab", ".gnosis-prefab",
            ".gg-shader", ".shader",
            ".gg-script", ".script",
            ".gon", ".json", ".csv", ".tsv",
            ".gnosis-asset", ".gnosis-mesh", ".gnosis-audio"
        };

        var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => supportedExtensions.Contains(Path.GetExtension(f)));

        foreach (var file in files)
        {
            var assetPath = file.Replace('\\', '/');
            var formatType = InferFormatType(assetPath);

            var node = new BuildNode
            {
                AssetPath = assetPath,
                FormatType = formatType,
                Status = BuildNodeStatus.Pending
            };

            _graph.AddNode(node);
        }
    }

    public void BuildDependencyGraph()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var node in _graph.GetAllNodes())
        {
            var deps = _baker.CollectDependencies(node.AssetPath);

            foreach (var dep in deps)
            {
                _graph.AddDependency(node.AssetPath, dep);
            }
        }

        if (_graph.HasCyclicDependency())
        {
            var cycle = _graph.GetCyclePath();
            throw new InvalidOperationException($"依赖图中存在循环依赖：{string.Join(" -> ", cycle)}");
        }
    }

    public async Task<BuildPipelineResult> BuildAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var buildOrder = _graph.GetBuildOrder();
        var results = new List<BakeResult>();
        var failedAssets = new List<string>();
        var succeededCount = 0;
        var upToDateCount = 0;
        var skippedCount = 0;

        foreach (var assetPath in buildOrder)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var node = _graph.GetNode(assetPath);
            if (node is null)
            {
                continue;
            }

            if (_options.EnableIncrementalBuild && _buildCache is not null && _buildCache.IsUpToDate(assetPath))
            {
                var cachedOutput = _buildCache.GetOutputPath(assetPath);
                if (cachedOutput is not null && File.Exists(cachedOutput))
                {
                    _graph.UpdateNodeStatus(assetPath, BuildNodeStatus.UpToDate, cachedOutput);
                    upToDateCount++;
                    continue;
                }
            }

            var deps = _graph.GetDirectDependencies(assetPath);
            var hasFailedDep = deps.Any(dep =>
            {
                var depNode = _graph.GetNode(dep);
                return depNode is not null && depNode.Status == BuildNodeStatus.Failed;
            });

            if (hasFailedDep)
            {
                _graph.UpdateNodeStatus(assetPath, BuildNodeStatus.Skipped, errorMessage: "依赖资产构建失败");
                skippedCount++;
                continue;
            }

            _graph.UpdateNodeStatus(assetPath, BuildNodeStatus.Building);

            var result = await _baker.BakeAsync(assetPath, _options.OutputRoot, cancellationToken);
            results.Add(result);

            if (result.Success)
            {
                _graph.UpdateNodeStatus(assetPath, BuildNodeStatus.Succeeded, result.OutputPath);
                succeededCount++;
            }
            else
            {
                _graph.UpdateNodeStatus(assetPath, BuildNodeStatus.Failed, errorMessage: result.ErrorMessage);
                failedAssets.Add(assetPath);

                if (_options.FailOnFirstError)
                {
                    break;
                }
            }
        }

        stopwatch.Stop();

        _buildCache?.Save();

        return new BuildPipelineResult
        {
            TotalAssets = _graph.NodeCount,
            SucceededCount = succeededCount,
            FailedCount = failedAssets.Count,
            UpToDateCount = upToDateCount,
            SkippedCount = skippedCount,
            TotalDuration = stopwatch.Elapsed,
            Results = results,
            FailedAssets = failedAssets
        };
    }

    public void InvalidateAsset(string assetPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _buildCache?.Invalidate(assetPath);
        _buildCache?.InvalidateDependents(assetPath);
        _graph.UpdateNodeStatus(assetPath, BuildNodeStatus.Pending);
    }

    public void InvalidateAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _buildCache?.Clear();
        _graph.ResetAllStatus();
    }

    private static FormatType InferFormatType(string assetPath)
    {
        var extension = Path.GetExtension(assetPath).ToLowerInvariant();

        return extension switch
        {
            ".png" or ".jpg" or ".jpeg" or ".ktx" or ".tga" or ".bmp" or ".hdr"
                => FormatType.Texture,
            ".obj" or ".gltf" or ".glb" or ".fbx" or ".gnosis-mesh"
                => FormatType.Mesh,
            ".wav" or ".mp3" or ".ogg" or ".flac" or ".gnosis-audio"
                => FormatType.Audio,
            ".gnosis-anim" or ".anim"
                => FormatType.Animation,
            ".gnosis-mat" or ".mat"
                => FormatType.Material,
            ".scene"
                => FormatType.Scene,
            ".prefab" or ".gnosis-prefab"
                => FormatType.Prefab,
            ".gg-shader" or ".shader"
                => FormatType.Shader,
            ".gg-script" or ".script"
                => FormatType.Script,
            ".gon" or ".json" or ".csv" or ".tsv"
                => FormatType.Config,
            ".gnosis-asset"
                => FormatType.Asset,
            ".svg" or ".gnosis-svg"
                => FormatType.VectorGraphic,
            _ => FormatType.Unknown
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _buildCache?.Dispose();
        _disposed = true;
    }
}

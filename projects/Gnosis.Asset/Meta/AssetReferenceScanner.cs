using Gnosis.Asset.Format;
using Gnosis.Asset.Meta;
using Gnosis.Asset.VFS;

namespace Gnosis.Asset.Meta;

public sealed class AssetReferenceScanner
{
    private readonly IVirtualFileSystem _vfs;
    private readonly FormatRegistry _formatRegistry;
    private readonly AssetGuidRegistry _guidRegistry;

    public AssetReferenceScanner(IVirtualFileSystem vfs, FormatRegistry formatRegistry, AssetGuidRegistry guidRegistry)
    {
        _vfs = vfs ?? throw new ArgumentNullException(nameof(vfs));
        _formatRegistry = formatRegistry ?? throw new ArgumentNullException(nameof(formatRegistry));
        _guidRegistry = guidRegistry ?? throw new ArgumentNullException(nameof(guidRegistry));
    }

    #region 引用图构建

    /// <summary>
    /// 扫描指定路径下的所有资产并构建引用图
    /// </summary>
    public async Task<AssetReferenceScanResult> ScanAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(rootPath))
        {
            throw new ArgumentNullException(nameof(rootPath));
        }

        var normalizedRoot = NormalizePath(rootPath);
        var assetPaths = DiscoverAssets(normalizedRoot);
        var referenceGraph = new AssetDependencyGraph();
        var brokenReferences = new List<BrokenReference>();
        var assetReferenceMap = new Dictionary<string, IReadOnlyList<string>>();

        foreach (var assetPath in assetPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            referenceGraph.AddAsset(assetPath);

            var references = await CollectReferencesAsync(assetPath, cancellationToken);
            assetReferenceMap[assetPath] = references;

            foreach (var refPath in references)
            {
                if (_vfs.FileExists(refPath))
                {
                    referenceGraph.AddDependency(assetPath, refPath);
                }
                else
                {
                    brokenReferences.Add(new BrokenReference
                    {
                        SourceAsset = assetPath,
                        ReferencedPath = refPath,
                        Reason = BrokenReferenceReason.AssetNotFound
                    });
                }
            }
        }

        var deadAssets = DetectDeadAssets(referenceGraph, assetPaths);

        return new AssetReferenceScanResult
        {
            TotalAssetCount = assetPaths.Count,
            ReferenceGraph = referenceGraph,
            AssetReferences = assetReferenceMap,
            BrokenReferences = brokenReferences,
            DeadAssets = deadAssets
        };
    }

    /// <summary>
    /// 扫描指定资产列表并构建引用图
    /// </summary>
    public async Task<AssetReferenceScanResult> ScanAsync(IEnumerable<string> assetPaths, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assetPaths);

        var assetList = assetPaths.ToList();
        var referenceGraph = new AssetDependencyGraph();
        var brokenReferences = new List<BrokenReference>();
        var assetReferenceMap = new Dictionary<string, IReadOnlyList<string>>();

        foreach (var assetPath in assetList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedPath = NormalizePath(assetPath);
            referenceGraph.AddAsset(normalizedPath);

            var references = await CollectReferencesAsync(normalizedPath, cancellationToken);
            assetReferenceMap[normalizedPath] = references;

            foreach (var refPath in references)
            {
                if (_vfs.FileExists(refPath))
                {
                    referenceGraph.AddDependency(normalizedPath, refPath);
                }
                else
                {
                    brokenReferences.Add(new BrokenReference
                    {
                        SourceAsset = normalizedPath,
                        ReferencedPath = refPath,
                        Reason = BrokenReferenceReason.AssetNotFound
                    });
                }
            }
        }

        var deadAssets = DetectDeadAssets(referenceGraph, assetList);

        return new AssetReferenceScanResult
        {
            TotalAssetCount = assetList.Count,
            ReferenceGraph = referenceGraph,
            AssetReferences = assetReferenceMap,
            BrokenReferences = brokenReferences,
            DeadAssets = deadAssets
        };
    }

    #endregion

    #region 死资产检测

    /// <summary>
    /// 检测未被任何资产引用的死资产
    /// </summary>
    public IReadOnlyList<string> DetectDeadAssets(AssetDependencyGraph graph, IReadOnlyList<string> allAssetPaths)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(allAssetPaths);

        var deadAssets = new List<string>();

        foreach (var assetPath in allAssetPaths)
        {
            var normalized = NormalizePath(assetPath);

            if (!graph.ContainsAsset(normalized))
            {
                continue;
            }

            var reverseDeps = graph.GetReverseDependencies(normalized);

            if (reverseDeps.Count == 0)
            {
                deadAssets.Add(normalized);
            }
        }

        return deadAssets;
    }

    /// <summary>
    /// 检测死资产，排除指定的根资产
    /// </summary>
    public IReadOnlyList<string> DetectDeadAssets(AssetDependencyGraph graph, IReadOnlyList<string> allAssetPaths, HashSet<string> rootAssets)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(allAssetPaths);
        ArgumentNullException.ThrowIfNull(rootAssets);

        var deadAssets = new List<string>();

        foreach (var assetPath in allAssetPaths)
        {
            var normalized = NormalizePath(assetPath);

            if (rootAssets.Contains(normalized))
            {
                continue;
            }

            if (!graph.ContainsAsset(normalized))
            {
                continue;
            }

            var reverseDeps = graph.GetReverseDependencies(normalized);

            if (reverseDeps.Count == 0)
            {
                deadAssets.Add(normalized);
            }
        }

        return deadAssets;
    }

    #endregion

    #region 引用修复

    /// <summary>
    /// 修复断裂的资产引用
    /// </summary>
    public IReadOnlyList<ReferenceRepairResult> RepairReferences(IReadOnlyList<BrokenReference> brokenReferences, ReferenceRepairStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(brokenReferences);

        var results = new List<ReferenceRepairResult>();

        foreach (var broken in brokenReferences)
        {
            var repairResult = strategy switch
            {
                ReferenceRepairStrategy.SearchByFilename => RepairByFilename(broken),
                ReferenceRepairStrategy.SearchByGuid => RepairByGuid(broken),
                ReferenceRepairStrategy.RemoveReference => RepairByRemoval(broken),
                _ => new ReferenceRepairResult
                {
                    BrokenReference = broken,
                    Success = false,
                    Message = $"未知的修复策略：{strategy}"
                }
            };

            results.Add(repairResult);
        }

        return results;
    }

    #endregion

    #region 私有方法

    private List<string> DiscoverAssets(string rootPath)
    {
        var assets = new List<string>();

        try
        {
            var files = _vfs.GetFiles(rootPath, "*");
            assets.AddRange(files);
        }
        catch (DirectoryNotFoundException)
        {
        }

        return assets;
    }

    private async Task<IReadOnlyList<string>> CollectReferencesAsync(string assetPath, CancellationToken cancellationToken)
    {
        var handler = _formatRegistry.GetHandler(assetPath);

        if (handler is IAssetFormat assetFormat)
        {
            try
            {
                var references = await assetFormat.GetReferencedAssetsAsync(assetPath, cancellationToken);
                return references.ToList();
            }
            catch (FileNotFoundException)
            {
                return Array.Empty<string>();
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
            catch (InvalidDataException)
            {
                return Array.Empty<string>();
            }
        }

        return Array.Empty<string>();
    }

    private ReferenceRepairResult RepairByFilename(BrokenReference broken)
    {
        var fileName = Path.GetFileName(broken.ReferencedPath);

        var candidates = new List<string>();

        foreach (var (guid, path) in _guidRegistry.GetAllMappings())
        {
            if (Path.GetFileName(path).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(path);
            }
        }

        if (candidates.Count == 1)
        {
            return new ReferenceRepairResult
            {
                BrokenReference = broken,
                Success = true,
                RepairedPath = candidates[0],
                Message = $"通过文件名匹配找到唯一候选：{candidates[0]}"
            };
        }

        if (candidates.Count > 1)
        {
            return new ReferenceRepairResult
            {
                BrokenReference = broken,
                Success = false,
                Message = $"找到 {candidates.Count} 个同名文件候选，无法自动修复"
            };
        }

        return new ReferenceRepairResult
        {
            BrokenReference = broken,
            Success = false,
            Message = "未找到匹配的文件名"
        };
    }

    private ReferenceRepairResult RepairByGuid(BrokenReference broken)
    {
        if (broken.ReferencedGuid.HasValue && !broken.ReferencedGuid.Value.IsEmpty)
        {
            if (_guidRegistry.TryGetPath(broken.ReferencedGuid.Value, out var path))
            {
                return new ReferenceRepairResult
                {
                    BrokenReference = broken,
                    Success = true,
                    RepairedPath = path,
                    Message = $"通过 GUID 匹配找到资产：{path}"
                };
            }
        }

        return new ReferenceRepairResult
        {
            BrokenReference = broken,
            Success = false,
            Message = "无法通过 GUID 定位资产"
        };
    }

    private ReferenceRepairResult RepairByRemoval(BrokenReference broken)
    {
        return new ReferenceRepairResult
        {
            BrokenReference = broken,
            Success = true,
            RepairedPath = null,
            Message = "标记为移除断裂引用"
        };
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    #endregion
}

public sealed class AssetReferenceScanResult
{
    public int TotalAssetCount { get; init; }
    public AssetDependencyGraph ReferenceGraph { get; init; } = new();
    public IReadOnlyDictionary<string, IReadOnlyList<string>> AssetReferences { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
    public IReadOnlyList<BrokenReference> BrokenReferences { get; init; } = [];
    public IReadOnlyList<string> DeadAssets { get; init; } = [];
}

public sealed class BrokenReference
{
    public string SourceAsset { get; init; } = string.Empty;
    public string ReferencedPath { get; init; } = string.Empty;
    public AssetGuid? ReferencedGuid { get; init; }
    public BrokenReferenceReason Reason { get; init; }
}

public enum BrokenReferenceReason
{
    AssetNotFound = 0,
    GuidMismatch = 1,
    HashMismatch = 2
}

public sealed class ReferenceRepairResult
{
    public BrokenReference BrokenReference { get; init; } = null!;
    public bool Success { get; init; }
    public string? RepairedPath { get; init; }
    public string Message { get; init; } = string.Empty;
}

public enum ReferenceRepairStrategy
{
    SearchByFilename = 0,
    SearchByGuid = 1,
    RemoveReference = 2
}

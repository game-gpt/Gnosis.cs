using Gnosis.Asset.Cache;
using Gnosis.Asset.Format;
using Gnosis.Asset.Meta;
using Gnosis.Toolchain.AssetPipeline.Graph;

namespace Gnosis.Toolchain.AssetPipeline.Bake;

public sealed class AssetBaker
{
    private readonly List<IBakeRule> _rules = new();
    private readonly IFormatRegistry _formatRegistry;
    private readonly BuildCache? _buildCache;

    public AssetBaker(IFormatRegistry formatRegistry, BuildCache? buildCache = null)
    {
        _formatRegistry = formatRegistry ?? throw new ArgumentNullException(nameof(formatRegistry));
        _buildCache = buildCache;
    }

    public void RegisterRule(IBakeRule rule)
    {
        if (rule is null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        _rules.Add(rule);
    }

    public bool UnregisterRule(IBakeRule rule)
    {
        return _rules.Remove(rule);
    }

    public async Task<BakeResult> BakeAsync(string assetPath, string outputDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return BakeResult.Failed(assetPath, FormatType.Unknown, "资产路径不能为空");
        }

        if (!File.Exists(assetPath))
        {
            return BakeResult.Failed(assetPath, FormatType.Unknown, $"资产文件不存在：{assetPath}");
        }

        var formatType = InferFormatType(assetPath);
        var rule = FindRule(assetPath, formatType);

        if (rule is null)
        {
            return BakeResult.Failed(assetPath, formatType, $"未找到支持资产 {assetPath} 的烘焙规则");
        }

        if (_buildCache is not null && _buildCache.IsUpToDate(assetPath))
        {
            var cachedOutput = _buildCache.GetOutputPath(assetPath);
            if (cachedOutput is not null && File.Exists(cachedOutput))
            {
                return BakeResult.UpToDate(assetPath, cachedOutput, formatType);
            }
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = await rule.BakeAsync(assetPath, outputDirectory, _formatRegistry, cancellationToken);
            stopwatch.Stop();

            if (result.Success && _buildCache is not null && result.OutputPath is not null)
            {
                _buildCache.MarkBuilt(assetPath, result.OutputPath, result.Dependencies);
            }

            return result with { Duration = stopwatch.Elapsed };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            return BakeResult.Failed(assetPath, formatType, $"烘焙 IO 错误：{ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return BakeResult.Failed(assetPath, formatType, $"烘焙权限错误：{ex.Message}");
        }
    }

    public async Task<IReadOnlyList<BakeResult>> BakeAllAsync(
        IEnumerable<string> assetPaths,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var results = new List<BakeResult>();

        foreach (var assetPath in assetPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await BakeAsync(assetPath, outputDirectory, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    public IReadOnlyList<string> CollectDependencies(string assetPath)
    {
        var formatType = InferFormatType(assetPath);
        var rule = FindRule(assetPath, formatType);
        return rule?.CollectDependencies(assetPath, _formatRegistry) ?? [];
    }

    private IBakeRule? FindRule(string assetPath, FormatType formatType)
    {
        foreach (var rule in _rules)
        {
            if (rule.CanBake(assetPath, formatType))
            {
                return rule;
            }
        }

        return null;
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
}

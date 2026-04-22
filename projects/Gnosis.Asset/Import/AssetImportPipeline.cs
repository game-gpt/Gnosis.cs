using Gnosis.Asset.Format;

namespace Gnosis.Asset.Import;

public sealed class AssetImportPipeline
{
    private readonly List<IAssetImporter> _importers = new();

    public void RegisterImporter(IAssetImporter importer)
    {
        if (importer is null)
        {
            throw new ArgumentNullException(nameof(importer));
        }

        _importers.Add(importer);
    }

    public bool UnregisterImporter(IAssetImporter importer)
    {
        return _importers.Remove(importer);
    }

    public async Task<ImportResult> ImportAsync(
        string assetPath,
        ImportContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return ImportResult.Failed(assetPath, "资产路径不能为空");
        }

        var formatType = InferFormatType(assetPath);

        foreach (var importer in _importers)
        {
            if (importer.CanImport(assetPath, formatType))
            {
                return await importer.ImportAsync(context, assetPath, cancellationToken);
            }
        }

        context.Logger?.LogWarning($"未找到支持资产 {assetPath} 的导入器");
        return ImportResult.Failed(assetPath, $"未找到支持该资产格式的导入器");
    }

    public async Task<IReadOnlyList<ImportResult>> ImportAllAsync(
        IEnumerable<string> assetPaths,
        ImportContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ImportResult>();

        foreach (var assetPath in assetPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await ImportAsync(assetPath, context, cancellationToken);
            results.Add(result);
        }

        return results;
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
            _ => FormatType.Unknown
        };
    }
}

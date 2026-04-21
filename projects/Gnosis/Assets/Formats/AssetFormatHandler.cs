using System.Text.Json;

namespace Gnosis.Assets.Formats;

public class AssetFormatHandler : FormatHandlerBase, IAssetFormat
{
    public override FormatType SupportedFormat => FormatType.Asset;
    
    public async Task<AssetData> LoadAssetAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<AssetData>(data) 
            ?? throw new InvalidOperationException($"Failed to deserialize asset: {path}");
    }
    
    public async Task SaveAssetAsync(string path, AssetData asset, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(asset, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await WriteAsync(path, data, null, cancellationToken);
    }
    
    public async Task<IEnumerable<string>> GetReferencedAssetsAsync(string path, CancellationToken cancellationToken = default)
    {
        var asset = await LoadAssetAsync(path, cancellationToken);
        var references = new List<string>();
        
        foreach (var metadata in asset.Metadata)
        {
            if (metadata.Value.StartsWith("asset://"))
            {
                references.Add(metadata.Value);
            }
        }
        
        return references;
    }
    
    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".gnosis-asset", ".asset", ".scirpta" };
    }
}

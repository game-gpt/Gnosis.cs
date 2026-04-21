namespace Gnosis.Assets.Formats;

public interface IAssetFormat : IFormatHandler
{
    Task<AssetData> LoadAssetAsync(string path, CancellationToken cancellationToken = default);
    Task SaveAssetAsync(string path, AssetData asset, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetReferencedAssetsAsync(string path, CancellationToken cancellationToken = default);
}

public record AssetData
{
    public string Name { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public byte[] RawData { get; init; } = [];
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

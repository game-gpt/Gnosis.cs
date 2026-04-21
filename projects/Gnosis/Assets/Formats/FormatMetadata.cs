namespace Gnosis.Assets.Formats;

public record FormatMetadata
{
    public FormatType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public long Size { get; init; }
    public CompressionType Compression { get; init; }
    public string Checksum { get; init; } = string.Empty;
    public int Version { get; init; }
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> Dependencies { get; init; } = new List<string>();
}

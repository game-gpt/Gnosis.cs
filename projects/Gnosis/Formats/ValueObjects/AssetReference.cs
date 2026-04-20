using Gnosis.Core.ValueObjects;

namespace Gnosis.Formats.ValueObjects;

public record AssetReference
{
    public EntityId AssetId { get; init; }
    public string AssetPath { get; init; } = string.Empty;
    public string AssetType { get; init; } = string.Empty;
    public bool IsLoaded { get; init; }
    public int ReferenceCount { get; init; }
}

using Gnosis.ECS.Core;

namespace Gnosis.Assets.Formats;

public record AssetReference
{
    public EntityId AssetId { get; init; }
    public string AssetPath { get; init; } = string.Empty;
    public string AssetType { get; init; } = string.Empty;
    public bool IsLoaded { get; init; }
    public int ReferenceCount { get; init; }
}

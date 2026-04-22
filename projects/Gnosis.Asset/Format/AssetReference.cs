using Gnosis.Core;
using Gnosis.Core.Event;

namespace Gnosis.Asset.Format;

public record AssetReference
{
    public EntityId AssetId { get; init; }
    public string AssetPath { get; init; } = string.Empty;
    public string AssetType { get; init; } = string.Empty;
    public bool IsLoaded { get; init; }
    public int ReferenceCount { get; init; }
}

using Gnosis.Core;

namespace Gnosis.Assets.Formats;

public interface IPrefabFormat : IFormatHandler
{
    Task<PrefabData> LoadPrefabAsync(string path, CancellationToken cancellationToken = default);
    Task SavePrefabAsync(string path, PrefabData prefab, CancellationToken cancellationToken = default);
    Task<EntityId> InstantiateAsync(string path, CancellationToken cancellationToken = default);
    Task<IEnumerable<AssetReference>> GetDependenciesAsync(string path, CancellationToken cancellationToken = default);
}

public record PrefabData
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<ComponentData> Components { get; init; } = new List<ComponentData>();
    public IReadOnlyList<PrefabData> Children { get; init; } = new List<PrefabData>();
    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
}

public record ComponentData
{
    public string TypeName { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object> Fields { get; init; } = new Dictionary<string, object>();
}

using Gnosis.Core;

namespace Gnosis.Assets.Formats;

public interface ISceneFormat : IFormatHandler
{
    Task<SceneData> LoadSceneAsync(string path, CancellationToken cancellationToken = default);
    Task SaveSceneAsync(string path, SceneData scene, CancellationToken cancellationToken = default);
    Task<IEnumerable<EntityId>> GetRootEntitiesAsync(string path, CancellationToken cancellationToken = default);
}

public record SceneData
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<EntityData> Entities { get; init; } = new List<EntityData>();
    public IReadOnlyList<SceneSystemData> Systems { get; init; } = new List<SceneSystemData>();
    public IReadOnlyDictionary<string, string> EnvironmentSettings { get; init; } = new Dictionary<string, string>();
}

public record EntityData
{
    public EntityId Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public IReadOnlyList<ComponentData> Components { get; init; } = new List<ComponentData>();
    public IReadOnlyList<EntityData> Children { get; init; } = new List<EntityData>();
}

public record SceneSystemData
{
    public string TypeName { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
    public IReadOnlyDictionary<string, object> Settings { get; init; } = new Dictionary<string, object>();
}

using System.Text.Json;
using Gnosis.ECS.Core;

namespace Gnosis.Assets.Formats;

public class SceneFormatHandler : FormatHandlerBase, ISceneFormat
{
    public override FormatType SupportedFormat => FormatType.Scene;
    
    public async Task<SceneData> LoadSceneAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<SceneData>(data) 
            ?? throw new InvalidOperationException($"Failed to deserialize scene: {path}");
    }
    
    public async Task SaveSceneAsync(string path, SceneData scene, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(scene, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await WriteAsync(path, data, null, cancellationToken);
    }
    
    public async Task<IEnumerable<EntityId>> GetRootEntitiesAsync(string path, CancellationToken cancellationToken = default)
    {
        var scene = await LoadSceneAsync(path, cancellationToken);
        return scene.Entities.Where(e => e.Id != EntityId.Empty).Select(e => e.Id);
    }
    
    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".gnosis-scene", ".scene" };
    }
}

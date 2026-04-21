using System.Text.Json;
using Gnosis.Core;

namespace Gnosis.Assets.Formats;

public class PrefabFormatHandler : FormatHandlerBase, IPrefabFormat
{
    public override FormatType SupportedFormat => FormatType.Prefab;
    
    public async Task<PrefabData> LoadPrefabAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<PrefabData>(data) 
            ?? throw new InvalidOperationException($"Failed to deserialize prefab: {path}");
    }
    
    public async Task SavePrefabAsync(string path, PrefabData prefab, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(prefab, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await WriteAsync(path, data, null, cancellationToken);
    }
    
    public async Task<EntityId> InstantiateAsync(string path, CancellationToken cancellationToken = default)
    {
        var prefab = await LoadPrefabAsync(path, cancellationToken);
        return await CreateEntityFromPrefab(prefab, cancellationToken);
    }
    
    public async Task<IEnumerable<AssetReference>> GetDependenciesAsync(string path, CancellationToken cancellationToken = default)
    {
        var prefab = await LoadPrefabAsync(path, cancellationToken);
        var dependencies = new List<AssetReference>();
        
        CollectDependencies(prefab, dependencies);
        
        return dependencies;
    }
    
    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".gnosis-prefab", ".prefab", ".scirptp" };
    }
    
    private Task<EntityId> CreateEntityFromPrefab(PrefabData prefab, CancellationToken cancellationToken)
    {
        var entityId = EntityId.New();
        return Task.FromResult(entityId);
    }
    
    private void CollectDependencies(PrefabData prefab, List<AssetReference> dependencies)
    {
        foreach (var component in prefab.Components)
        {
            foreach (var field in component.Fields)
            {
                if (field.Value is string path && path.StartsWith("asset://"))
                {
                    dependencies.Add(new AssetReference
                    {
                        AssetPath = path,
                        AssetType = "Unknown",
                        IsLoaded = false,
                        ReferenceCount = 1
                    });
                }
            }
        }
        
        foreach (var child in prefab.Children)
        {
            CollectDependencies(child, dependencies);
        }
    }
}

using System.Text.Json;
using Gnosis.Formats.Enums;
using Gnosis.Formats.Interfaces;

namespace Gnosis.Formats.Implementations;

public class MeshFormatHandler : FormatHandlerBase, IMeshFormat
{
    public override FormatType SupportedFormat => FormatType.Mesh;
    
    public async Task<MeshData> LoadMeshAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<MeshData>(data) 
            ?? throw new InvalidOperationException($"Failed to deserialize mesh: {path}");
    }
    
    public async Task SaveMeshAsync(string path, MeshData mesh, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(mesh, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await WriteAsync(path, data, null, cancellationToken);
    }
    
    public Task<MeshData> OptimizeAsync(MeshData mesh, MeshOptimizationOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new MeshOptimizationOptions();
        
        var optimizedMesh = mesh with { };
        
        return Task.FromResult(optimizedMesh);
    }
    
    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".mesh", ".ggmesh", ".obj", ".fbx", ".gltf", ".glb" };
    }
}

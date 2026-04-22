using System.Text.Json;

namespace Gnosis.Asset.Format;

public class AnimationFormatHandler : FormatHandlerBase, IAnimationFormat
{
    public override FormatType SupportedFormat => FormatType.Animation;
    
    public async Task<AnimationData> LoadAnimationAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<AnimationData>(data) 
            ?? throw new InvalidOperationException($"Failed to deserialize animation: {path}");
    }
    
    public async Task SaveAnimationAsync(string path, AnimationData animation, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(animation, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await WriteAsync(path, data, null, cancellationToken);
    }
    
    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".gnosis-anim", ".anim", ".fbx", ".gltf", ".glb", ".scirptanim" };
    }
}

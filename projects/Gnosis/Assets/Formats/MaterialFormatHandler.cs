using System.Text.Json;

namespace Gnosis.Assets.Formats;

public class MaterialFormatHandler : FormatHandlerBase, IMaterialFormat
{
    public override FormatType SupportedFormat => FormatType.Material;
    
    public async Task<MaterialData> LoadMaterialAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<MaterialData>(data) 
            ?? throw new InvalidOperationException($"Failed to deserialize material: {path}");
    }
    
    public async Task SaveMaterialAsync(string path, MaterialData material, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(material, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await WriteAsync(path, data, null, cancellationToken);
    }
    
    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".mat", ".material", ".scirptmat" };
    }
}

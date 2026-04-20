using System.Text.Json;
using Gnosis.Formats.Enums;
using Gnosis.Formats.Interfaces;

namespace Gnosis.Formats.Implementations;

public class TextureFormatHandler : FormatHandlerBase, ITextureFormat
{
    public override FormatType SupportedFormat => FormatType.Texture;
    
    public async Task<TextureData> LoadTextureAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<TextureData>(data) 
            ?? throw new InvalidOperationException($"Failed to deserialize texture: {path}");
    }
    
    public async Task SaveTextureAsync(string path, TextureData texture, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(texture, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await WriteAsync(path, data, null, cancellationToken);
    }
    
    public Task<TextureData> ResizeAsync(TextureData texture, int width, int height, CancellationToken cancellationToken = default)
    {
        var resizedTexture = texture with 
        { 
            Width = width, 
            Height = height 
        };
        
        return Task.FromResult(resizedTexture);
    }
    
    public Task<TextureData> CompressAsync(TextureData texture, TextureCompressionFormat format, CancellationToken cancellationToken = default)
    {
        var compressedTexture = texture with { };
        
        return Task.FromResult(compressedTexture);
    }
    
    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".hdr", ".exr", ".dds", ".ktx", ".ggtexture" };
    }
}

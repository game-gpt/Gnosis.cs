using System.Text.Json;
using Gnosis.Assets.Formats;

namespace Gnosis.Rendering.Shader;

public class ShaderFormatHandler : FormatHandlerBase, IShaderFormat
{
    public override FormatType SupportedFormat => FormatType.Shader;

    public async Task<ShaderData> LoadShaderAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<ShaderData>(data)
            ?? throw new InvalidOperationException($"Failed to deserialize shader: {path}");
    }

    public async Task SaveShaderAsync(string path, ShaderData shader, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(shader, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await WriteAsync(path, data, null, cancellationToken);
    }

    public Task<byte[]> CompileAsync(ShaderData shader, ShaderTarget target, CancellationToken cancellationToken = default)
    {
        var sourceBytes = System.Text.Encoding.UTF8.GetBytes(shader.SourceCode);

        return Task.FromResult(sourceBytes);
    }

    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string> { ".shader", ".glsl", ".hlsl", ".vert", ".frag", ".comp" };
    }
}

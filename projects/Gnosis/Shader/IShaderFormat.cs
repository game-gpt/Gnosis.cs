using Gnosis.Formats;
using GnosisEngine.Shader.ValueObjects;

namespace GnosisEngine.Shader;

public interface IShaderFormat : IFormatHandler
{
    Task<ShaderData> LoadShaderAsync(string path, CancellationToken cancellationToken = default);
    Task SaveShaderAsync(string path, ShaderData shader, CancellationToken cancellationToken = default);
    Task<byte[]> CompileAsync(ShaderData shader, ShaderTarget target, CancellationToken cancellationToken = default);
}

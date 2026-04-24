using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

public interface IShaderFormat
{
    Task<ShaderData> LoadShaderAsync(string path, CancellationToken cancellationToken = default);
    Task SaveShaderAsync(string path, ShaderData shader, CancellationToken cancellationToken = default);
    Task<byte[]> CompileAsync(ShaderData shader, ShaderTarget target, CancellationToken cancellationToken = default);
}

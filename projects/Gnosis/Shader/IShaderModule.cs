using Gnosis.Shader.ValueObjects;

namespace Gnosis.Shader;

public interface IShaderModule : IDisposable
{
    string Name { get; }
    IReadOnlyList<IMicroFunction> Functions { get; }
    byte[] Bytecode { get; }
    ShaderLanguage Language { get; }
    ShaderTarget Target { get; }
}

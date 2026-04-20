using Gnosis.Shader.ValueObjects;

namespace Gnosis.Shader;

public interface IShaderBackend
{
    string Name { get; }

    bool SupportsKind(MicroFunctionKind kind);
    IShaderModule CompileModule(IShaderModule module, ShaderCompileOptions options);
    void DispatchCompute(IMicroFunction computeFunction, uint groupCountX, uint groupCountY, uint groupCountZ);
}

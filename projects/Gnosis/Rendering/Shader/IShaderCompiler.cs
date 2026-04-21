namespace Gnosis.Rendering.Shader;

public interface IShaderCompiler
{
    ShaderTarget Target { get; }

    IShaderModule Compile(string sourceCode, string moduleName, ShaderCompileOptions options);
    bool Validate(string sourceCode, out string errorMessage);
}

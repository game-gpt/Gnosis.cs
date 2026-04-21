namespace Gnosis.Rendering.Shader;

public interface IMicroFunction
{
    string Name { get; }
    MicroFunctionKind Kind { get; }
    IReadOnlyList<ShaderParameter> InputParameters { get; }
    ShaderParameter OutputParameter { get; }
    IReadOnlyList<ShaderUniform> Uniforms { get; }
    IReadOnlyList<ShaderAttribute> Attributes { get; }
    IReadOnlyList<ShaderSampler> Samplers { get; }
}

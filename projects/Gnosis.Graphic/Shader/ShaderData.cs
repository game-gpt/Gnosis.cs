using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

public record ShaderData
{
    public string Name { get; init; } = string.Empty;
    public ShaderStage Stage { get; init; }
    public string SourceCode { get; init; } = string.Empty;
    public ShaderLanguage Language { get; init; }
    public IReadOnlyList<ShaderUniform> Uniforms { get; init; } = new List<ShaderUniform>();
    public IReadOnlyList<ShaderAttribute> Attributes { get; init; } = new List<ShaderAttribute>();
    public IReadOnlyList<ShaderSampler> Samplers { get; init; } = new List<ShaderSampler>();
    public IReadOnlyDictionary<string, string> Macros { get; init; } = new Dictionary<string, string>();
}

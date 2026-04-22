namespace Gnosis.Graphic.Shader;

public record ShaderAttribute
{
    public string Name { get; init; } = string.Empty;
    public ShaderAttributeType Type { get; init; }
    public int Location { get; init; }
    public int SemanticIndex { get; init; }
}

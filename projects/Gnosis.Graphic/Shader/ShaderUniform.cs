namespace Gnosis.Graphic.Shader;

public record ShaderUniform
{
    public string Name { get; init; } = string.Empty;
    public ShaderUniformType Type { get; init; }
    public int ArraySize { get; init; } = 1;
    public int Binding { get; init; }
    public int Set { get; init; }
    public object? DefaultValue { get; init; }
}

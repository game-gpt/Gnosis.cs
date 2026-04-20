namespace GnosisEngine.Shader.ValueObjects;

public record ShaderSampler
{
    public string Name { get; init; } = string.Empty;
    public int Binding { get; init; }
    public int Set { get; init; }
    public ShaderSamplerType Type { get; init; }
}

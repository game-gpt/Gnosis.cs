namespace GnosisEngine.Shader.ValueObjects;

public record ShaderParameter
{
    public string Name { get; init; } = string.Empty;
    public ShaderParameterType Type { get; init; }
    public int Location { get; init; }
    public int ArraySize { get; init; } = 1;
}

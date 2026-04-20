namespace GnosisEngine.Shader.ValueObjects;

public record ShaderCompileOptions
{
    public IReadOnlyDictionary<string, string> Defines { get; init; } = new Dictionary<string, string>();
    public OptimizationLevel OptimizationLevel { get; init; } = OptimizationLevel.Default;
    public bool DebugInfo { get; init; }
}

public enum OptimizationLevel
{
    None = 0,
    Default = 1,
    Maximum = 2
}

namespace Gnosis.Formats;

public interface IShaderFormat : IFormatHandler
{
    Task<ShaderData> LoadShaderAsync(string path, CancellationToken cancellationToken = default);
    Task SaveShaderAsync(string path, ShaderData shader, CancellationToken cancellationToken = default);
    Task<byte[]> CompileAsync(ShaderData shader, ShaderTarget target, CancellationToken cancellationToken = default);
}

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

public record ShaderUniform
{
    public string Name { get; init; } = string.Empty;
    public ShaderUniformType Type { get; init; }
    public int ArraySize { get; init; } = 1;
    public int Binding { get; init; }
    public int Set { get; init; }
    public object? DefaultValue { get; init; }
}

public record ShaderAttribute
{
    public string Name { get; init; } = string.Empty;
    public ShaderAttributeType Type { get; init; }
    public int Location { get; init; }
    public int SemanticIndex { get; init; }
}

public record ShaderSampler
{
    public string Name { get; init; } = string.Empty;
    public int Binding { get; init; }
    public int Set { get; init; }
    public ShaderSamplerType Type { get; init; }
}

public enum ShaderStage
{
    Vertex = 0,
    Fragment = 1,
    Geometry = 2,
    TessControl = 3,
    TessEvaluation = 4,
    Compute = 5,
    RayGen = 6,
    RayAnyHit = 7,
    RayClosestHit = 8,
    RayMiss = 9,
    RayIntersection = 10
}

public enum ShaderLanguage
{
    Unknown = 0,
    GLSL = 1,
    HLSL = 2,
    SPIRV = 3,
    MSL = 4,
    WGSL = 5,
    GGShader = 6
}

public enum ShaderTarget
{
    SPIRV = 0,
    DXIL = 1,
    MSL = 2,
    WGSL = 3,
    GLSL = 4,
    HLSL = 5
}

public enum ShaderUniformType
{
    Unknown = 0,
    Float = 1,
    Float2 = 2,
    Float3 = 3,
    Float4 = 4,
    Int = 5,
    Int2 = 6,
    Int3 = 7,
    Int4 = 8,
    Matrix3x3 = 9,
    Matrix4x4 = 10,
    Texture2D = 11,
    TextureCube = 12,
    Texture3D = 13,
    Sampler = 14,
    StorageBuffer = 15,
    UniformBuffer = 16
}

public enum ShaderAttributeType
{
    Unknown = 0,
    Float = 1,
    Float2 = 2,
    Float3 = 3,
    Float4 = 4,
    Int = 5,
    Int2 = 6,
    Int3 = 7,
    Int4 = 8,
    UByte4 = 9,
    UByte4Normalized = 10
}

public enum ShaderSamplerType
{
    Sampler1D = 0,
    Sampler2D = 1,
    Sampler3D = 2,
    SamplerCube = 3,
    Sampler2DArray = 4,
    SamplerCubeArray = 5
}

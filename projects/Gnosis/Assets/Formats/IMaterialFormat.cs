namespace Gnosis.Assets.Formats;

public interface IMaterialFormat : IFormatHandler
{
    Task<MaterialData> LoadMaterialAsync(string path, CancellationToken cancellationToken = default);
    Task SaveMaterialAsync(string path, MaterialData material, CancellationToken cancellationToken = default);
}

public record MaterialData
{
    public string Name { get; init; } = string.Empty;
    public string ShaderPath { get; init; } = string.Empty;
    public IReadOnlyList<MaterialProperty> Properties { get; init; } = new List<MaterialProperty>();
    public IReadOnlyList<MaterialTexture> Textures { get; init; } = new List<MaterialTexture>();
    public BlendMode BlendMode { get; init; }
    public bool IsTransparent { get; init; }
    public bool DoubleSided { get; init; }
}

public record MaterialProperty
{
    public string Name { get; init; } = string.Empty;
    public MaterialPropertyType Type { get; init; }
    public object Value { get; init; } = new object();
}

public record MaterialTexture
{
    public string Name { get; init; } = string.Empty;
    public string TexturePath { get; init; } = string.Empty;
    public int UvSet { get; init; }
}

public enum MaterialPropertyType
{
    Float = 0,
    Float2 = 1,
    Float3 = 2,
    Float4 = 3,
    Int = 4,
    Bool = 5,
    Matrix = 6,
    Texture = 7
}

public enum BlendMode
{
    None,
    Alpha,
    Additive,
    Multiply,
    Screen
}

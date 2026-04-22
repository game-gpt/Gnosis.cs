namespace Gnosis.Graphic.Material;

public enum MaterialPropertyType
{
    Float,
    Float2,
    Float3,
    Float4,
    Int,
    Matrix4x4,
    Texture2D,
    TextureCube,
    Sampler,
    Buffer
}

public readonly record struct MaterialPropertyKey(string Name, MaterialPropertyType Type)
{
    public override string ToString() => $"{Name}({Type})";
}

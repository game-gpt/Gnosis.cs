namespace GnosisEngine.Shader.ValueObjects;

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

namespace Gnosis.Rendering.Shader;

/// <summary>
/// 着色器参数类型枚举
/// </summary>
public enum ShaderParameterType
{
    Unknown = 0,
    Bool = 1,
    Int32 = 2,
    UInt32 = 3,
    Float32 = 4,
    Float64 = 5,
    Vec2 = 6,
    Vec3 = 7,
    Vec4 = 8,
    Mat2 = 9,
    Mat3 = 10,
    Mat4 = 11,
    Texture2D = 12,
    TextureCube = 13,
    Texture3D = 14,
    Sampler = 15,
    StorageBuffer = 16,
    UniformBuffer = 17,
    AccelerationStructure = 18,
    NeuralModel = 19,
    DiffusionModel = 20,
    TensorF16 = 21,
    TensorF32 = 22,
    TensorBF16 = 23,
    TensorInt8 = 24,
    TensorInt4 = 25
}

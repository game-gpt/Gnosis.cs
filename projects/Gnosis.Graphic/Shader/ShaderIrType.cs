using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader IR 类型枚举（已迁移至 Gnosis.IR.Shader）
/// </summary>
public enum ShaderIrType
{
    Void,
    Bool,
    Int32,
    Int64,
    UInt32,
    UInt64,
    Float32,
    Float64,
    Vector2,
    Vector3,
    Vector4,
    Matrix3x3,
    Matrix4x4,
    Array,
    Struct,
    Pointer,
    Function,
    Sampler,
    Image,
    SampledImage,
    AccelerationStructure
}

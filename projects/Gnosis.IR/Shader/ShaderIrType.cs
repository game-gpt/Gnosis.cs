namespace Gnosis.IR.Shader;

/// <summary>
/// Shader IR 类型枚举
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

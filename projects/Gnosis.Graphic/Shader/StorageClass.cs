using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// 存储类枚举（已迁移至 Gnosis.IR.Shader）
/// </summary>
public enum StorageClass
{
    UniformConstant,
    Input,
    Uniform,
    Output,
    Workgroup,
    CrossWorkgroup,
    Private,
    Function,
    Generic,
    PushConstant,
    AtomicCounter,
    Image,
    StorageBuffer,
    PhysicalStorageBuffer,
    RayPayload,
    HitAttribute,
    CallableData,
    IncomingRayPayload,
    IncomingCallableData,
    ShaderRecordBuffer
}

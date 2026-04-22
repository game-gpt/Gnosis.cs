namespace Gnosis.IR.Shader;

/// <summary>
/// 存储类枚举
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

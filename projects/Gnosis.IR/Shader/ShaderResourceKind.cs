namespace Gnosis.IR.Shader;

/// <summary>
/// Shader 资源种类枚举
/// </summary>
public enum ShaderResourceKind
{
    UniformBuffer,
    StorageBuffer,
    Image,
    Sampler,
    SampledImage,
    StorageImage,
    InputAttachment,
    AccelerationStructure,
    RayPayload,
    HitAttribute,
    CallableData,
    ShaderRecordBuffer
}

using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader 资源种类枚举（已迁移至 Gnosis.IR.Shader）
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

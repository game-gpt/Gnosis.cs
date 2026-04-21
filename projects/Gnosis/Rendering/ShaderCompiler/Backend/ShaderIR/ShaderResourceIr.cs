namespace Gnosis.Rendering.ShaderCompiler.Backend.ShaderIR;

public sealed record ShaderResourceIr(
    string Name,
    ShaderResourceKind Kind,
    uint DescriptorSet,
    uint Binding,
    ShaderIrType Type,
    uint? SpecConstantId = null)
{
    public uint ResultId { get; set; }
}

public enum ShaderResourceKind
{
    UniformBuffer,
    StorageBuffer,
    Texture,
    Sampler,
    SampledImage,
    PushConstant,
    SpecConstant,
    AccelerationStructure
}

namespace Gnosis.Graphic.RHI;

/// <summary>
/// 描述符集绑定描述
/// </summary>
public record DescriptorSetBinding
{
    public required uint Binding { get; init; }
    public required DescriptorType DescriptorType { get; init; }
    public uint DescriptorCount { get; init; } = 1;
    public ShaderStageFlag StageFlags { get; init; } = ShaderStageFlag.AllGraphics;
}

/// <summary>
/// 描述符类型
/// </summary>
public enum DescriptorType
{
    UniformBuffer = 0,
    StorageBuffer = 1,
    CombinedImageSampler = 2,
    SampledImage = 3,
    StorageImage = 4,
    Sampler = 5,
    InputAttachment = 6
}

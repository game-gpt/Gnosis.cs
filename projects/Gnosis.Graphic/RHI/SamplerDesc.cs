namespace Gnosis.Graphic.RHI;

/// <summary>
/// 采样器描述
/// </summary>
public record SamplerDesc
{
    public FilterMode MagFilter { get; init; } = FilterMode.Linear;
    public FilterMode MinFilter { get; init; } = FilterMode.Linear;
    public SamplerAddressMode AddressModeU { get; init; } = SamplerAddressMode.Repeat;
    public SamplerAddressMode AddressModeV { get; init; } = SamplerAddressMode.Repeat;
    public SamplerAddressMode AddressModeW { get; init; } = SamplerAddressMode.Repeat;
    public float MinLod { get; init; } = 0.0f;
    public float MaxLod { get; init; } = float.MaxValue;
    public float MaxAnisotropy { get; init; } = 1.0f;
    public bool CompareEnable { get; init; }
    public CompareFunction CompareOp { get; init; } = CompareFunction.Always;
}

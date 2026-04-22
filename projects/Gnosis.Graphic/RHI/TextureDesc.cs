namespace Gnosis.Graphic.RHI;

/// <summary>
/// 纹理资源描述
/// </summary>
public record TextureDesc
{
    public required TextureDimension Dimension { get; init; }
    public required uint Width { get; init; }
    public uint Height { get; init; } = 1;
    public uint Depth { get; init; } = 1;
    public uint MipLevels { get; init; } = 1;
    public uint ArrayLayers { get; init; } = 1;
    public required ResourceFormat Format { get; init; }
    public TextureUsage Usage { get; init; }
    public uint SampleCount { get; init; } = 1;
}

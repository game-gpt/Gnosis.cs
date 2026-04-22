using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Pipeline;

public record ResourceDesc
{
    public uint Width { get; init; }
    public uint Height { get; init; }
    public ResourceFormat Format { get; init; }
    public ResourceUsage Usage { get; init; }
}

[Flags]
public enum ResourceUsage
{
    None = 0,
    RenderTarget = 1 << 0,
    DepthStencil = 1 << 1,
    ShaderResource = 1 << 2,
    UnorderedAccess = 1 << 3,
    TransferSrc = 1 << 4,
    TransferDst = 1 << 5
}

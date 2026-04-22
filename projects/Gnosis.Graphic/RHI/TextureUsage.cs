namespace Gnosis.Graphic.RHI;

/// <summary>
/// 纹理用途标志
/// </summary>
[Flags]
public enum TextureUsage
{
    None = 0,
    ShaderResource = 1 << 0,
    RenderTarget = 1 << 1,
    DepthStencil = 1 << 2,
    UnorderedAccess = 1 << 3,
    TransferSrc = 1 << 4,
    TransferDst = 1 << 5,
    InputAttachment = 1 << 6
}

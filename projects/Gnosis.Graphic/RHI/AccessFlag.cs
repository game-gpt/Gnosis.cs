namespace Gnosis.Graphic.RHI;

/// <summary>
/// 内存访问标志
/// </summary>
[Flags]
public enum AccessFlag
{
    None = 0,
    IndirectCommandRead = 1 << 0,
    IndexRead = 1 << 1,
    VertexAttributeRead = 1 << 2,
    UniformRead = 1 << 3,
    InputAttachmentRead = 1 << 4,
    ShaderRead = 1 << 5,
    ShaderWrite = 1 << 6,
    ColorAttachmentRead = 1 << 7,
    ColorAttachmentWrite = 1 << 8,
    DepthStencilAttachmentRead = 1 << 9,
    DepthStencilAttachmentWrite = 1 << 10,
    TransferRead = 1 << 11,
    TransferWrite = 1 << 12,
    HostRead = 1 << 13,
    HostWrite = 1 << 14,
    MemoryRead = 1 << 15,
    MemoryWrite = 1 << 16
}

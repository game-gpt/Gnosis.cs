namespace Gnosis.Graphic.RHI;

/// <summary>
/// 缓冲区用途标志
/// </summary>
[Flags]
public enum BufferUsage
{
    None = 0,
    VertexBuffer = 1 << 0,
    IndexBuffer = 1 << 1,
    UniformBuffer = 1 << 2,
    StorageBuffer = 1 << 3,
    TransferSrc = 1 << 4,
    TransferDst = 1 << 5,
    IndirectBuffer = 1 << 6
}

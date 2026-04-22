namespace Gnosis.Graphic.RHI;

/// <summary>
/// 缓冲区资源描述
/// </summary>
public record BufferDesc
{
    public required ulong Size { get; init; }
    public BufferUsage Usage { get; init; }
    public bool HostVisible { get; init; }
    public bool DeviceLocal { get; init; } = true;
    public byte[]? InitialData { get; init; }
}

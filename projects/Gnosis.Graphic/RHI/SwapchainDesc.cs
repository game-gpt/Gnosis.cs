namespace Gnosis.Graphic.RHI;

/// <summary>
/// 交换链描述
/// </summary>
public record SwapchainDesc
{
    public required nint WindowHandle { get; init; }
    public required uint Width { get; init; }
    public required uint Height { get; init; }
    public ResourceFormat Format { get; init; } = ResourceFormat.B8G8R8A8Unorm;
    public uint ImageCount { get; init; } = 3;
    public PresentMode PresentMode { get; init; } = PresentMode.Fifo;
    public bool VSync { get; init; } = true;
}

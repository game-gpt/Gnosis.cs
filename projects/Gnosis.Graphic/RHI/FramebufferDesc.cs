namespace Gnosis.Graphic.RHI;

/// <summary>
/// 帧缓冲描述
/// </summary>
public record FramebufferDesc
{
    public required IRhiRenderPass RenderPass { get; init; }
    public IResource[] Attachments { get; init; } = [];
    public required uint Width { get; init; }
    public required uint Height { get; init; }
    public uint Layers { get; init; } = 1;
}

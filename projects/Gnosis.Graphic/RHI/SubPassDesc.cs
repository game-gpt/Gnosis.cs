namespace Gnosis.Graphic.RHI;

/// <summary>
/// 渲染子通道描述
/// </summary>
public record SubPassDesc
{
    public uint[] InputAttachments { get; init; } = [];
    public uint[] ColorAttachments { get; init; } = [];
    public uint DepthStencilAttachment { get; init; } = uint.MaxValue;
    public uint[] ResolveAttachments { get; init; } = [];
}

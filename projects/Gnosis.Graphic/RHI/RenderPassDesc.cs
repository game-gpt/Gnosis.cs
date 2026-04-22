namespace Gnosis.Graphic.RHI;

/// <summary>
/// 渲染通道描述
/// </summary>
public record RenderPassDesc
{
    public AttachmentDesc[] Attachments { get; init; } = [];
    public SubPassDesc[] SubPasses { get; init; } = [];
    public SubPassDependency[] Dependencies { get; init; } = [];
}

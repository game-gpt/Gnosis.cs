namespace Gnosis.Graphic.RHI;

/// <summary>
/// 渲染通道接口，定义附件引用和子通道描述
/// </summary>
public interface IRhiRenderPass : IDisposable
{
    /// <summary>
    /// 原生句柄
    /// </summary>
    nint Handle { get; }

    /// <summary>
    /// 附件数量
    /// </summary>
    uint AttachmentCount { get; }

    /// <summary>
    /// 子通道数量
    /// </summary>
    uint SubPassCount { get; }
}

namespace Gnosis.Graphic.RHI;

/// <summary>
///     帧缓冲接口，绑定附件资源到渲染通道
/// </summary>
public interface IRhiFramebuffer : IDisposable
{
    /// <summary>
    ///     帧缓冲宽度
    /// </summary>
    uint Width { get; }

    /// <summary>
    ///     帧缓冲高度
    /// </summary>
    uint Height { get; }

    /// <summary>
    ///     附件资源列表
    /// </summary>
    IReadOnlyList<IResource> Attachments { get; }

    /// <summary>
    ///     关联的渲染通道
    /// </summary>
    IRhiRenderPass RenderPass { get; }
}

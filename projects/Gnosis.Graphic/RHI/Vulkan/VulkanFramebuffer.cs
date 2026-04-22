namespace Gnosis.Graphic.RHI.Vulkan;

/// <summary>
/// Vulkan 帧缓冲实现
/// </summary>
internal sealed class VulkanFramebuffer : RHI.IRhiFramebuffer
{
    /// <summary>
    /// Vulkan 帧缓冲句柄
    /// </summary>
    public VkFramebuffer Handle { get; }

    /// <summary>
    /// 原生句柄
    /// </summary>
    nint RHI.IRhiFramebuffer.Handle => (nint)Handle;

    /// <summary>
    /// 帧缓冲宽度
    /// </summary>
    public uint Width { get; }

    /// <summary>
    /// 帧缓冲高度
    /// </summary>
    public uint Height { get; }

    /// <summary>
    /// 附件资源列表
    /// </summary>
    public IReadOnlyList<RHI.IResource> Attachments { get; }

    /// <summary>
    /// 关联的渲染通道
    /// </summary>
    public RHI.IRhiRenderPass RenderPass { get; }

    /// <summary>
    /// 关联的逻辑设备
    /// </summary>
    private readonly VkDevice _device;

    /// <summary>
    /// 是否已释放
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// 创建 Vulkan 帧缓冲
    /// </summary>
    /// <param name="device">逻辑设备</param>
    /// <param name="handle">帧缓冲句柄</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="attachments">附件列表</param>
    /// <param name="renderPass">渲染通道</param>
    public VulkanFramebuffer(VkDevice device, VkFramebuffer handle, uint width, uint height, IReadOnlyList<RHI.IResource> attachments, RHI.IRhiRenderPass renderPass)
    {
        _device = device;
        Handle = handle;
        Width = width;
        Height = height;
        Attachments = attachments;
        RenderPass = renderPass;
    }

    /// <summary>
    /// 释放帧缓冲
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        VulkanNative.vkDestroyFramebuffer(_device, Handle, null);
    }
}

namespace Gnosis.Graphic.RHI.Vulkan;

/// <summary>
/// Vulkan 渲染通道实现
/// </summary>
internal sealed unsafe class VulkanRenderPass : RHI.IRhiRenderPass
{
    /// <summary>
    /// Vulkan 渲染通道句柄
    /// </summary>
    public VkRenderPass Handle { get; }

    public uint AttachmentCount { get; }

    /// <summary>
    /// 子通道数量
    /// </summary>
    public uint SubPassCount { get; }

    /// <summary>
    /// 关联的逻辑设备
    /// </summary>
    private readonly VkDevice _device;

    /// <summary>
    /// 是否已释放
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// 创建 Vulkan 渲染通道
    /// </summary>
    /// <param name="device">逻辑设备</param>
    /// <param name="handle">渲染通道句柄</param>
    /// <param name="attachmentCount">附件数量</param>
    /// <param name="subPassCount">子通道数量</param>
    public VulkanRenderPass(VkDevice device, VkRenderPass handle, uint attachmentCount, uint subPassCount)
    {
        _device = device;
        Handle = handle;
        AttachmentCount = attachmentCount;
        SubPassCount = subPassCount;
    }

    /// <summary>
    /// 释放渲染通道
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        VulkanNative.vkDestroyRenderPass(_device, Handle, null);
    }
}

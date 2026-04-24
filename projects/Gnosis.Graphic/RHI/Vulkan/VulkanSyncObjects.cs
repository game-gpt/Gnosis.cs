namespace Gnosis.Graphic.RHI.Vulkan;

internal sealed unsafe class VulkanFence : RHI.IRhiFence
{
    /// <summary>
    /// Vulkan 栅栏句柄
    /// </summary>
    public VkFence Handle { get; }

    /// <summary>
    /// 关联的逻辑设备
    /// </summary>
    private readonly VkDevice _device;

    /// <summary>
    /// 是否已释放
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// 栅栏是否已触发
    /// </summary>
    public bool IsSignaled
    {
        get
        {
            var handle = Handle;
            var result = VulkanNative.vkWaitForFences(_device, 1, &handle, true, 0);
            return result == VkResult.Success;
        }
    }

    /// <summary>
    /// 创建 Vulkan 栅栏
    /// </summary>
    /// <param name="device">逻辑设备</param>
    /// <param name="handle">栅栏句柄</param>
    public VulkanFence(VkDevice device, VkFence handle)
    {
        _device = device;
        Handle = handle;
    }

    /// <summary>
    /// 等待栅栏触发
    /// </summary>
    /// <param name="timeout">超时时间（纳秒）</param>
    public void Wait(ulong timeout = ulong.MaxValue)
    {
        var handle = Handle;
        VulkanNative.CheckResult(
            VulkanNative.vkWaitForFences(_device, 1, &handle, true, timeout),
            "等待栅栏");
    }

    /// <summary>
    /// 重置栅栏为未触发状态
    /// </summary>
    public void Reset()
    {
        var handle = Handle;
        VulkanNative.CheckResult(
            VulkanNative.vkResetFences(_device, 1, &handle),
            "重置栅栏");
    }

    /// <summary>
    /// 释放栅栏
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        VulkanNative.vkDestroyFence(_device, Handle, null);
    }
}

/// <summary>
/// Vulkan 信号量实现
/// </summary>
internal sealed unsafe class VulkanSemaphore : RHI.IRhiSemaphore
{
    /// <summary>
    /// Vulkan 信号量句柄
    /// </summary>
    public VkSemaphore Handle { get; }

    /// <summary>
    /// 关联的逻辑设备
    /// </summary>
    private readonly VkDevice _device;

    /// <summary>
    /// 是否已释放
    /// </summary>
    private bool _isDisposed;

    /// <summary>
    /// 创建 Vulkan 信号量
    /// </summary>
    /// <param name="device">逻辑设备</param>
    /// <param name="handle">信号量句柄</param>
    public VulkanSemaphore(VkDevice device, VkSemaphore handle)
    {
        _device = device;
        Handle = handle;
    }

    /// <summary>
    /// 释放信号量
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        VulkanNative.vkDestroySemaphore(_device, Handle, null);
    }
}

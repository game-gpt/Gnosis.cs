using Gnosis.Graphic.Shader;

namespace Gnosis.Graphic.RHI.Vulkan;

/// <summary>
/// Vulkan 资源实现
/// </summary>
internal sealed unsafe class VulkanResource : RHI.IResource
{
    private static ulong _nextId = 1;

    #region IResource 属性

    /// <summary>
    /// 资源唯一标识
    /// </summary>
    public ulong Id { get; }

    /// <summary>
    /// 资源类型
    /// </summary>
    public RHI.ResourceType ResourceType { get; }

    /// <summary>
    /// 资源格式
    /// </summary>
    public RHI.ResourceFormat Format { get; }

    /// <summary>
    /// 资源大小（字节）
    /// </summary>
    public ulong Size { get; }

    /// <summary>
    /// 是否已释放
    /// </summary>
    public bool IsDisposed => _isDisposed;

    #endregion

    #region Vulkan 句柄

    /// <summary>
    /// 缓冲区句柄
    /// </summary>
    public VkBuffer BufferHandle { get; }

    /// <summary>
    /// 图像句柄
    /// </summary>
    public VkImage ImageHandle { get; }

    /// <summary>
    /// 图像视图句柄
    /// </summary>
    public VkImageView ImageViewHandle { get; }

    /// <summary>
    /// 采样器句柄
    /// </summary>
    public VkSampler SamplerHandle { get; }

    /// <summary>
    /// 着色器模块句柄
    /// </summary>
    public VkShaderModule ShaderModuleHandle { get; }

    /// <summary>
    /// 设备内存句柄
    /// </summary>
    public VkDeviceMemory DeviceMemory { get; }

    /// <summary>
    /// 着色器阶段（仅着色器资源有效）
    /// </summary>
    public ShaderStage ShaderStage { get; }

    /// <summary>
    /// 着色器入口点名称（仅着色器资源有效）
    /// </summary>
    public string EntryPoint { get; } = "main";

    #endregion

    #region 内部状态

    private bool _isDisposed;
    private readonly VkDevice _device;
    private readonly bool _ownsResources;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建缓冲区资源
    /// </summary>
    public VulkanResource(VkDevice device, VkBuffer buffer, VkDeviceMemory memory, ulong size, RHI.ResourceFormat format)
    {
        _device = device;
        _ownsResources = true;
        Id = _nextId++;
        ResourceType = RHI.ResourceType.Buffer;
        Format = format;
        Size = size;
        BufferHandle = buffer;
        DeviceMemory = memory;
        ImageHandle = VkImage.Null;
        ImageViewHandle = VkImageView.Null;
        SamplerHandle = VkSampler.Null;
        ShaderModuleHandle = VkShaderModule.Null;
    }

    /// <summary>
    /// 创建纹理资源
    /// </summary>
    public VulkanResource(VkDevice device, VkImage image, VkImageView imageView, VkDeviceMemory memory, ulong size, RHI.ResourceFormat format, RHI.ResourceType resourceType)
    {
        _device = device;
        _ownsResources = true;
        Id = _nextId++;
        ResourceType = resourceType;
        Format = format;
        Size = size;
        ImageHandle = image;
        ImageViewHandle = imageView;
        DeviceMemory = memory;
        BufferHandle = VkBuffer.Null;
        SamplerHandle = VkSampler.Null;
        ShaderModuleHandle = VkShaderModule.Null;
    }

    /// <summary>
    /// 创建采样器资源
    /// </summary>
    public VulkanResource(VkDevice device, VkSampler sampler)
    {
        _device = device;
        _ownsResources = true;
        Id = _nextId++;
        ResourceType = RHI.ResourceType.Sampler;
        Format = RHI.ResourceFormat.Unknown;
        Size = 0;
        SamplerHandle = sampler;
        BufferHandle = VkBuffer.Null;
        ImageHandle = VkImage.Null;
        ImageViewHandle = VkImageView.Null;
        ShaderModuleHandle = VkShaderModule.Null;
        DeviceMemory = VkDeviceMemory.Null;
    }

    /// <summary>
    /// 创建着色器资源
    /// </summary>
    public VulkanResource(VkDevice device, VkShaderModule shaderModule, ulong size, ShaderStage stage, string entryPoint)
    {
        _device = device;
        _ownsResources = true;
        Id = _nextId++;
        ResourceType = RHI.ResourceType.Shader;
        Format = RHI.ResourceFormat.Unknown;
        Size = size;
        ShaderModuleHandle = shaderModule;
        ShaderStage = stage;
        EntryPoint = entryPoint;
        BufferHandle = VkBuffer.Null;
        ImageHandle = VkImage.Null;
        ImageViewHandle = VkImageView.Null;
        SamplerHandle = VkSampler.Null;
        DeviceMemory = VkDeviceMemory.Null;
    }

    /// <summary>
    /// 创建不拥有 Vulkan 对象的资源（用于交换链图像）
    /// </summary>
    public VulkanResource(VkDevice device, VkImage image, VkImageView imageView, RHI.ResourceFormat format, RHI.ResourceType resourceType)
    {
        _device = device;
        _ownsResources = false;
        Id = _nextId++;
        ResourceType = resourceType;
        Format = format;
        Size = 0;
        ImageHandle = image;
        ImageViewHandle = imageView;
        BufferHandle = VkBuffer.Null;
        SamplerHandle = VkSampler.Null;
        ShaderModuleHandle = VkShaderModule.Null;
        DeviceMemory = VkDeviceMemory.Null;
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (!_ownsResources)
        {
            return;
        }

        switch (ResourceType)
        {
            case RHI.ResourceType.Buffer:
            {
                if (!BufferHandle.IsNull)
                {
                    VulkanNative.vkDestroyBuffer(_device, BufferHandle, null);
                }

                if (!DeviceMemory.IsNull)
                {
                    VulkanNative.vkFreeMemory(_device, DeviceMemory, null);
                }

                break;
            }

            case RHI.ResourceType.Texture1D:
            case RHI.ResourceType.Texture2D:
            case RHI.ResourceType.Texture3D:
            {
                if (!ImageViewHandle.IsNull)
                {
                    VulkanNative.vkDestroyImageView(_device, ImageViewHandle, null);
                }

                if (!ImageHandle.IsNull)
                {
                    VulkanNative.vkDestroyImage(_device, ImageHandle, null);
                }

                if (!DeviceMemory.IsNull)
                {
                    VulkanNative.vkFreeMemory(_device, DeviceMemory, null);
                }

                break;
            }

            case RHI.ResourceType.Sampler:
            {
                if (!SamplerHandle.IsNull)
                {
                    VulkanNative.vkDestroySampler(_device, SamplerHandle, null);
                }

                break;
            }

            case RHI.ResourceType.Shader:
            {
                if (!ShaderModuleHandle.IsNull)
                {
                    VulkanNative.vkDestroyShaderModule(_device, ShaderModuleHandle, null);
                }

                break;
            }
        }
    }

    #endregion
}

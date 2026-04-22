using System.Runtime.InteropServices;

namespace Gnosis.Graphic.RHI.Vulkan;

/// <summary>
/// Vulkan 描述符池句柄
/// </summary>
public readonly struct VkDescriptorPool : IEquatable<VkDescriptorPool>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkDescriptorPool Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 描述符池句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkDescriptorPool(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkDescriptorPool h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkDescriptorPool other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkDescriptorPool h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkDescriptorPool left, VkDescriptorPool right) => left.Equals(right);

    public static bool operator !=(VkDescriptorPool left, VkDescriptorPool right) => !left.Equals(right);
}

#region 描述符辅助结构

/// <summary>
/// 描述符池大小
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorPoolSize
{
    public VkDescriptorType Type;
    public uint DescriptorCount;
}

/// <summary>
/// 描述符池创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkDescriptorPoolCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint MaxSets;
    public uint PoolSizeCount;
    public VkDescriptorPoolSize* PPoolSizes;
}

/// <summary>
/// 描述符集分配信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkDescriptorSetAllocateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public VkDescriptorPool DescriptorPool;
    public uint DescriptorSetCount;
    public VkDescriptorSetLayout* PSetLayouts;
}

#endregion

/// <summary>
/// Vulkan 描述符集实现
/// </summary>
internal sealed unsafe class VulkanDescriptorSet : RHI.IRhiDescriptorSet
{
    #region 句柄属性

    /// <summary>
    /// Vulkan 描述符集句柄
    /// </summary>
    public VkDescriptorSet Handle { get; }

    /// <summary>
    /// Vulkan 描述符集布局句柄
    /// </summary>
    public VkDescriptorSetLayout Layout { get; }

    /// <summary>
    /// 绑定的管线布局
    /// </summary>
    public VkPipelineLayout BoundPipelineLayout { get; set; }

    #endregion

    #region 内部状态

    private readonly VkDevice _device;
    private readonly VkDescriptorPool _descriptorPool;
    private bool _isDisposed;

    private readonly List<DescriptorBinding> _pendingBindings = new();

    #endregion

    /// <summary>
    /// 创建 Vulkan 描述符集
    /// </summary>
    /// <param name="device">逻辑设备</param>
    /// <param name="descriptorPool">描述符池</param>
    /// <param name="handle">描述符集句柄</param>
    /// <param name="layout">描述符集布局句柄</param>
    public VulkanDescriptorSet(VkDevice device, VkDescriptorPool descriptorPool, VkDescriptorSet handle, VkDescriptorSetLayout layout)
    {
        _device = device;
        _descriptorPool = descriptorPool;
        Handle = handle;
        Layout = layout;
        BoundPipelineLayout = VkPipelineLayout.Null;
    }

    #region IRhiDescriptorSet 实现

    /// <summary>
    /// 绑定缓冲区到描述符集
    /// </summary>
    /// <param name="binding">绑定槽位</param>
    /// <param name="buffer">缓冲区资源</param>
    /// <param name="offset">偏移量</param>
    /// <param name="range">范围</param>
    public void BindBuffer(uint binding, RHI.IResource buffer, ulong offset = 0, ulong range = ulong.MaxValue)
    {
        var vkResource = (VulkanResource)buffer;
        _pendingBindings.Add(new DescriptorBinding
        {
            Binding = binding,
            Type = VkDescriptorType.UniformBuffer,
            BufferInfo = new VkDescriptorBufferInfo
            {
                Buffer = vkResource.BufferHandle,
                Offset = offset,
                Range = range == ulong.MaxValue ? vkResource.Size : range
            }
        });
    }

    /// <summary>
    /// 绑定纹理到描述符集
    /// </summary>
    /// <param name="binding">绑定槽位</param>
    /// <param name="texture">纹理资源</param>
    public void BindTexture(uint binding, RHI.IResource texture)
    {
        var vkResource = (VulkanResource)texture;
        _pendingBindings.Add(new DescriptorBinding
        {
            Binding = binding,
            Type = VkDescriptorType.SampledImage,
            ImageInfo = new VkDescriptorImageInfo
            {
                Sampler = VkSampler.Null,
                ImageView = vkResource.ImageViewHandle,
                ImageLayout = VkImageLayout.ShaderReadOnlyOptimal
            }
        });
    }

    /// <summary>
    /// 绑定采样器到描述符集
    /// </summary>
    /// <param name="binding">绑定槽位</param>
    /// <param name="sampler">采样器资源</param>
    public void BindSampler(uint binding, RHI.IResource sampler)
    {
        var vkResource = (VulkanResource)sampler;
        _pendingBindings.Add(new DescriptorBinding
        {
            Binding = binding,
            Type = VkDescriptorType.Sampler,
            ImageInfo = new VkDescriptorImageInfo
            {
                Sampler = vkResource.SamplerHandle,
                ImageView = VkImageView.Null,
                ImageLayout = VkImageLayout.Undefined
            }
        });
    }

    /// <summary>
    /// 绑定 Uniform 数据到描述符集
    /// </summary>
    /// <param name="binding">绑定槽位</param>
    /// <param name="data">数据指针</param>
    /// <param name="size">数据大小</param>
    public void BindUniformData(uint binding, void* data, ulong size)
    {
        _pendingBindings.Add(new DescriptorBinding
        {
            Binding = binding,
            Type = VkDescriptorType.UniformBuffer,
            IsUniformData = true,
            DataPtr = data,
            DataSize = size
        });
    }

    #endregion

    #region 刷新绑定

    /// <summary>
    /// 将所有挂起的绑定刷新到 Vulkan 描述符集
    /// </summary>
    public void Flush()
    {
        if (_pendingBindings.Count == 0)
        {
            return;
        }

        var writes = stackalloc VkWriteDescriptorSet[_pendingBindings.Count];

        for (var i = 0; i < _pendingBindings.Count; i++)
        {
            var binding = _pendingBindings[i];

            writes[i] = new VkWriteDescriptorSet
            {
                SType = VkStructureType.WriteDescriptorSet,
                PNext = null,
                DstSet = Handle,
                DstBinding = binding.Binding,
                DstArrayElement = 0,
                DescriptorCount = 1,
                DescriptorType = binding.Type,
                PImageInfo = null,
                PBufferInfo = null,
                PTexelBufferView = null
            };

            if (binding.Type == VkDescriptorType.UniformBuffer || binding.Type == VkDescriptorType.StorageBuffer)
            {
                var bufferInfo = binding.BufferInfo;
                writes[i].PBufferInfo = &bufferInfo;
            }
            else if (binding.Type == VkDescriptorType.Sampler ||
                     binding.Type == VkDescriptorType.SampledImage ||
                     binding.Type == VkDescriptorType.CombinedImageSampler)
            {
                var imageInfo = binding.ImageInfo;
                writes[i].PImageInfo = &imageInfo;
            }
        }

        VulkanNative.vkUpdateDescriptorSets(_device, (uint)_pendingBindings.Count, writes, 0, null);
        _pendingBindings.Clear();
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// 释放描述符集
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (!Layout.IsNull)
        {
            VulkanNative.vkDestroyDescriptorSetLayout(_device, Layout, null);
        }
    }

    #endregion

    #region 绑定信息

    /// <summary>
    /// 描述符绑定信息
    /// </summary>
    private struct DescriptorBinding
    {
        public uint Binding;
        public VkDescriptorType Type;
        public VkDescriptorBufferInfo BufferInfo;
        public VkDescriptorImageInfo ImageInfo;
        public bool IsUniformData;
        public void* DataPtr;
        public ulong DataSize;
    }

    #endregion
}

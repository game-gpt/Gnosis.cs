using System.Runtime.InteropServices;
using Gnosis.Graphic.Shader;

namespace Gnosis.Graphic.RHI.Vulkan;

#region 缺失的 Vulkan 类型

/// <summary>
/// Vulkan 物理设备类型
/// </summary>
internal enum VkPhysicalDeviceType
{
    Other = 0,
    IntegratedGpu = 1,
    DiscreteGpu = 2,
    VirtualGpu = 3,
    Cpu = 4
}

/// <summary>
/// Vulkan 队列标志位
/// </summary>
[Flags]
internal enum VkQueueFlagBits
{
    Graphics = 1,
    Compute = 2,
    Transfer = 4,
    SparseBinding = 8
}

/// <summary>
/// Vulkan 内存需求
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkMemoryRequirements
{
    public ulong Size;
    public ulong Alignment;
    public uint MemoryTypeBits;
}

/// <summary>
/// Vulkan 缓冲区复制区域
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkBufferCopy
{
    public ulong SrcOffset;
    public ulong DstOffset;
    public ulong Size;
}

/// <summary>
/// Vulkan 采样器 Mipmap 模式
/// </summary>
internal enum VkSamplerMipmapMode
{
    Nearest = 0,
    Linear = 1
}

/// <summary>
/// Vulkan 边框颜色
/// </summary>
internal enum VkBorderColor
{
    FloatTransparentBlack = 0,
    IntTransparentBlack = 1,
    FloatOpaqueBlack = 2,
    IntOpaqueBlack = 3,
    FloatOpaqueWhite = 4,
    IntOpaqueWhite = 5
}

/// <summary>
/// Vulkan 采样器创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkSamplerCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public VkFilter MagFilter;
    public VkFilter MinFilter;
    public VkSamplerMipmapMode MipmapMode;
    public VkSamplerAddressMode AddressModeU;
    public VkSamplerAddressMode AddressModeV;
    public VkSamplerAddressMode AddressModeW;
    public float MipLodBias;
    public uint AnisotropyEnable;
    public float MaxAnisotropy;
    public uint CompareEnable;
    public VkCompareOp CompareOp;
    public float MinLod;
    public float MaxLod;
    public VkBorderColor BorderColor;
    public uint UnnormalizedCoordinates;
}

#endregion

#region Vulkan 转换工具

/// <summary>
/// RHI 类型到 Vulkan 类型的转换工具
/// </summary>
internal static class VulkanConversions
{
    #region 格式转换

    /// <summary>
    /// 将 RHI 资源格式转换为 Vulkan 格式
    /// </summary>
    public static VkFormat ToVkFormat(RHI.ResourceFormat format)
    {
        return format switch
        {
            RHI.ResourceFormat.R8G8B8A8Unorm => VkFormat.R8G8B8A8Unorm,
            RHI.ResourceFormat.B8G8R8A8Unorm => VkFormat.B8G8R8A8Unorm,
            RHI.ResourceFormat.R16G16B16A16Float => VkFormat.R16G16B16A16Float,
            RHI.ResourceFormat.R32G32B32A32Float => VkFormat.R32G32B32A32Float,
            RHI.ResourceFormat.D24UnormS8Uint => VkFormat.D24UnormS8Uint,
            RHI.ResourceFormat.D32FloatS8Uint => VkFormat.D32FloatS8Uint,
            _ => VkFormat.Undefined
        };
    }

    /// <summary>
    /// 将 Vulkan 格式转换为 RHI 资源格式
    /// </summary>
    public static RHI.ResourceFormat ToResourceFormat(VkFormat format)
    {
        return format switch
        {
            VkFormat.R8G8B8A8Unorm => RHI.ResourceFormat.R8G8B8A8Unorm,
            VkFormat.B8G8R8A8Unorm => RHI.ResourceFormat.B8G8R8A8Unorm,
            VkFormat.R16G16B16A16Float => RHI.ResourceFormat.R16G16B16A16Float,
            VkFormat.R32G32B32A32Float => RHI.ResourceFormat.R32G32B32A32Float,
            VkFormat.D24UnormS8Uint => RHI.ResourceFormat.D24UnormS8Uint,
            VkFormat.D32FloatS8Uint => RHI.ResourceFormat.D32FloatS8Uint,
            _ => RHI.ResourceFormat.Unknown
        };
    }

    #endregion

    #region 缓冲区转换

    /// <summary>
    /// 将 RHI 缓冲区用途转换为 Vulkan 缓冲区用途
    /// </summary>
    public static VkBufferUsageFlagBits ToVkBufferUsage(RHI.BufferUsage usage)
    {
        var result = (VkBufferUsageFlagBits)0;

        if (usage.HasFlag(RHI.BufferUsage.VertexBuffer))
        {
            result |= VkBufferUsageFlagBits.VertexBuffer;
        }

        if (usage.HasFlag(RHI.BufferUsage.IndexBuffer))
        {
            result |= VkBufferUsageFlagBits.IndexBuffer;
        }

        if (usage.HasFlag(RHI.BufferUsage.UniformBuffer))
        {
            result |= VkBufferUsageFlagBits.UniformBuffer;
        }

        if (usage.HasFlag(RHI.BufferUsage.StorageBuffer))
        {
            result |= VkBufferUsageFlagBits.StorageBuffer;
        }

        if (usage.HasFlag(RHI.BufferUsage.TransferSrc))
        {
            result |= VkBufferUsageFlagBits.TransferSrc;
        }

        if (usage.HasFlag(RHI.BufferUsage.TransferDst))
        {
            result |= VkBufferUsageFlagBits.TransferDst;
        }

        if (usage.HasFlag(RHI.BufferUsage.IndirectBuffer))
        {
            result |= VkBufferUsageFlagBits.IndirectBuffer;
        }

        return result;
    }

    #endregion

    #region 纹理转换

    /// <summary>
    /// 将 RHI 纹理用途转换为 Vulkan 图像用途
    /// </summary>
    public static VkImageUsageFlagBits ToVkImageUsage(RHI.TextureUsage usage)
    {
        var result = (VkImageUsageFlagBits)0;

        if (usage.HasFlag(RHI.TextureUsage.ShaderResource))
        {
            result |= VkImageUsageFlagBits.Sampled;
        }

        if (usage.HasFlag(RHI.TextureUsage.RenderTarget))
        {
            result |= VkImageUsageFlagBits.ColorAttachment;
        }

        if (usage.HasFlag(RHI.TextureUsage.DepthStencil))
        {
            result |= VkImageUsageFlagBits.DepthStencilAttachment;
        }

        if (usage.HasFlag(RHI.TextureUsage.UnorderedAccess))
        {
            result |= VkImageUsageFlagBits.Storage;
        }

        if (usage.HasFlag(RHI.TextureUsage.TransferSrc))
        {
            result |= VkImageUsageFlagBits.TransferSrc;
        }

        if (usage.HasFlag(RHI.TextureUsage.TransferDst))
        {
            result |= VkImageUsageFlagBits.TransferDst;
        }

        if (usage.HasFlag(RHI.TextureUsage.InputAttachment))
        {
            result |= VkImageUsageFlagBits.InputAttachment;
        }

        return result;
    }

    /// <summary>
    /// 将 RHI 纹理维度转换为 Vulkan 图像类型
    /// </summary>
    public static VkImageType ToVkImageType(RHI.TextureDimension dimension)
    {
        return dimension switch
        {
            RHI.TextureDimension.Texture1D => VkImageType._1D,
            RHI.TextureDimension.Texture2D => VkImageType._2D,
            RHI.TextureDimension.Texture3D => VkImageType._3D,
            RHI.TextureDimension.TextureCube => VkImageType._2D,
            RHI.TextureDimension.Texture1DArray => VkImageType._1D,
            RHI.TextureDimension.Texture2DArray => VkImageType._2D,
            RHI.TextureDimension.TextureCubeArray => VkImageType._2D,
            _ => VkImageType._2D
        };
    }

    /// <summary>
    /// 将 RHI 纹理维度转换为 Vulkan 图像视图类型
    /// </summary>
    public static VkImageViewType ToVkImageViewType(RHI.TextureDimension dimension)
    {
        return dimension switch
        {
            RHI.TextureDimension.Texture1D => VkImageViewType._1D,
            RHI.TextureDimension.Texture2D => VkImageViewType._2D,
            RHI.TextureDimension.Texture3D => VkImageViewType._3D,
            RHI.TextureDimension.TextureCube => VkImageViewType.Cube,
            RHI.TextureDimension.Texture1DArray => VkImageViewType._1DArray,
            RHI.TextureDimension.Texture2DArray => VkImageViewType._2DArray,
            RHI.TextureDimension.TextureCubeArray => VkImageViewType.CubeArray,
            _ => VkImageViewType._2D
        };
    }

    /// <summary>
    /// 将 RHI 纹理维度转换为 RHI 资源类型
    /// </summary>
    public static RHI.ResourceType ToResourceType(RHI.TextureDimension dimension)
    {
        return dimension switch
        {
            RHI.TextureDimension.Texture1D => RHI.ResourceType.Texture1D,
            RHI.TextureDimension.Texture2D => RHI.ResourceType.Texture2D,
            RHI.TextureDimension.Texture3D => RHI.ResourceType.Texture3D,
            RHI.TextureDimension.TextureCube => RHI.ResourceType.Texture2D,
            RHI.TextureDimension.Texture1DArray => RHI.ResourceType.Texture1D,
            RHI.TextureDimension.Texture2DArray => RHI.ResourceType.Texture2D,
            RHI.TextureDimension.TextureCubeArray => RHI.ResourceType.Texture2D,
            _ => RHI.ResourceType.Texture2D
        };
    }

    /// <summary>
    /// 判断格式是否为深度模板格式
    /// </summary>
    public static bool IsDepthFormat(RHI.ResourceFormat format)
    {
        return format is RHI.ResourceFormat.D24UnormS8Uint or RHI.ResourceFormat.D32FloatS8Uint;
    }

    #endregion

    #region 管线状态转换

    /// <summary>
    /// 将 RHI 图元拓扑转换为 Vulkan 图元拓扑
    /// </summary>
    public static VkPrimitiveTopology ToVkPrimitiveTopology(RHI.PrimitiveTopology topology)
    {
        return topology switch
        {
            RHI.PrimitiveTopology.PointList => VkPrimitiveTopology.PointList,
            RHI.PrimitiveTopology.LineList => VkPrimitiveTopology.LineList,
            RHI.PrimitiveTopology.LineStrip => VkPrimitiveTopology.LineStrip,
            RHI.PrimitiveTopology.TriangleList => VkPrimitiveTopology.TriangleList,
            RHI.PrimitiveTopology.TriangleStrip => VkPrimitiveTopology.TriangleStrip,
            RHI.PrimitiveTopology.TriangleFan => VkPrimitiveTopology.TriangleFan,
            _ => VkPrimitiveTopology.TriangleList
        };
    }

    /// <summary>
    /// 将 RHI 剔除模式转换为 Vulkan 剔除模式
    /// </summary>
    public static VkCullModeFlags ToVkCullMode(RHI.CullMode cullMode)
    {
        return cullMode switch
        {
            RHI.CullMode.None => VkCullModeFlags.None,
            RHI.CullMode.Front => VkCullModeFlags.Front,
            RHI.CullMode.Back => VkCullModeFlags.Back,
            _ => VkCullModeFlags.None
        };
    }

    /// <summary>
    /// 将 RHI 正面朝向转换为 Vulkan 正面朝向
    /// </summary>
    public static VkFrontFace ToVkFrontFace(RHI.FrontFace frontFace)
    {
        return frontFace switch
        {
            RHI.FrontFace.CounterClockwise => VkFrontFace.CounterClockwise,
            RHI.FrontFace.Clockwise => VkFrontFace.Clockwise,
            _ => VkFrontFace.CounterClockwise
        };
    }

    /// <summary>
    /// 将 RHI 多边形模式转换为 Vulkan 多边形模式
    /// </summary>
    public static VkPolygonMode ToVkPolygonMode(RHI.PolygonMode polygonMode)
    {
        return polygonMode switch
        {
            RHI.PolygonMode.Fill => VkPolygonMode.Fill,
            RHI.PolygonMode.Line => VkPolygonMode.Line,
            RHI.PolygonMode.Point => VkPolygonMode.Point,
            _ => VkPolygonMode.Fill
        };
    }

    /// <summary>
    /// 将 RHI 比较函数转换为 Vulkan 比较操作
    /// </summary>
    public static VkCompareOp ToVkCompareOp(RHI.CompareFunction compareFunc)
    {
        return compareFunc switch
        {
            RHI.CompareFunction.Never => VkCompareOp.Never,
            RHI.CompareFunction.Less => VkCompareOp.Less,
            RHI.CompareFunction.Equal => VkCompareOp.Equal,
            RHI.CompareFunction.LessEqual => VkCompareOp.LessOrEqual,
            RHI.CompareFunction.Greater => VkCompareOp.Greater,
            RHI.CompareFunction.NotEqual => VkCompareOp.NotEqual,
            RHI.CompareFunction.GreaterEqual => VkCompareOp.GreaterOrEqual,
            RHI.CompareFunction.Always => VkCompareOp.Always,
            _ => VkCompareOp.Always
        };
    }

    /// <summary>
    /// 将 RHI 模板操作转换为 Vulkan 模板操作
    /// </summary>
    public static VkStencilOp ToVkStencilOp(RHI.StencilOp stencilOp)
    {
        return stencilOp switch
        {
            RHI.StencilOp.Keep => VkStencilOp.Keep,
            RHI.StencilOp.Zero => VkStencilOp.Zero,
            RHI.StencilOp.Replace => VkStencilOp.Replace,
            RHI.StencilOp.IncrementClamp => VkStencilOp.IncrementAndClamp,
            RHI.StencilOp.DecrementClamp => VkStencilOp.DecrementAndClamp,
            RHI.StencilOp.Invert => VkStencilOp.Invert,
            RHI.StencilOp.IncrementWrap => VkStencilOp.IncrementAndWrap,
            RHI.StencilOp.DecrementWrap => VkStencilOp.DecrementAndWrap,
            _ => VkStencilOp.Keep
        };
    }

    /// <summary>
    /// 将 RHI 模板操作状态转换为 Vulkan 模板操作状态
    /// </summary>
    public static VkStencilOpState ToVkStencilOpState(RHI.StencilOpState state)
    {
        return new VkStencilOpState
        {
            FailOp = ToVkStencilOp(state.FailOp),
            PassOp = ToVkStencilOp(state.PassOp),
            DepthFailOp = ToVkStencilOp(state.DepthFailOp),
            CompareOp = ToVkCompareOp(state.CompareOp),
            CompareMask = state.CompareMask,
            WriteMask = state.WriteMask,
            Reference = state.Reference
        };
    }

    /// <summary>
    /// 将 RHI 采样计数转换为 Vulkan 采样计数
    /// </summary>
    public static VkSampleCountFlagBits ToVkSampleCount(uint sampleCount)
    {
        return sampleCount switch
        {
            1 => VkSampleCountFlagBits._1,
            2 => VkSampleCountFlagBits._2,
            4 => VkSampleCountFlagBits._4,
            8 => VkSampleCountFlagBits._8,
            16 => VkSampleCountFlagBits._16,
            32 => VkSampleCountFlagBits._32,
            64 => VkSampleCountFlagBits._64,
            _ => VkSampleCountFlagBits._1
        };
    }

    #endregion

    #region 混合状态转换

    /// <summary>
    /// 将 RHI 混合因子转换为 Vulkan 混合因子
    /// </summary>
    public static VkBlendFactor ToVkBlendFactor(RHI.BlendFactor factor)
    {
        return factor switch
        {
            RHI.BlendFactor.Zero => VkBlendFactor.Zero,
            RHI.BlendFactor.One => VkBlendFactor.One,
            RHI.BlendFactor.SrcColor => VkBlendFactor.SrcColor,
            RHI.BlendFactor.OneMinusSrcColor => VkBlendFactor.OneMinusSrcColor,
            RHI.BlendFactor.DstColor => VkBlendFactor.DstColor,
            RHI.BlendFactor.OneMinusDstColor => VkBlendFactor.OneMinusDstColor,
            RHI.BlendFactor.SrcAlpha => VkBlendFactor.SrcAlpha,
            RHI.BlendFactor.OneMinusSrcAlpha => VkBlendFactor.OneMinusSrcAlpha,
            RHI.BlendFactor.DstAlpha => VkBlendFactor.DstAlpha,
            RHI.BlendFactor.OneMinusDstAlpha => VkBlendFactor.OneMinusDstAlpha,
            RHI.BlendFactor.ConstantColor => VkBlendFactor.ConstantColor,
            RHI.BlendFactor.OneMinusConstantColor => VkBlendFactor.OneMinusConstantColor,
            RHI.BlendFactor.ConstantAlpha => VkBlendFactor.ConstantAlpha,
            RHI.BlendFactor.OneMinusConstantAlpha => VkBlendFactor.OneMinusConstantAlpha,
            RHI.BlendFactor.SrcAlphaSaturate => VkBlendFactor.SrcAlphaSaturate,
            _ => VkBlendFactor.Zero
        };
    }

    /// <summary>
    /// 将 RHI 混合操作转换为 Vulkan 混合操作
    /// </summary>
    public static VkBlendOp ToVkBlendOp(RHI.BlendOp blendOp)
    {
        return blendOp switch
        {
            RHI.BlendOp.Add => VkBlendOp.Add,
            RHI.BlendOp.Subtract => VkBlendOp.Subtract,
            RHI.BlendOp.ReverseSubtract => VkBlendOp.ReverseSubtract,
            RHI.BlendOp.Min => VkBlendOp.Min,
            RHI.BlendOp.Max => VkBlendOp.Max,
            _ => VkBlendOp.Add
        };
    }

    /// <summary>
    /// 将 RHI 颜色写入掩码转换为 Vulkan 颜色分量标志
    /// </summary>
    public static VkColorComponentFlags ToVkColorComponentFlags(RHI.ColorWriteMask mask)
    {
        var result = (VkColorComponentFlags)0;

        if (mask.HasFlag(RHI.ColorWriteMask.Red))
        {
            result |= VkColorComponentFlags.R;
        }

        if (mask.HasFlag(RHI.ColorWriteMask.Green))
        {
            result |= VkColorComponentFlags.G;
        }

        if (mask.HasFlag(RHI.ColorWriteMask.Blue))
        {
            result |= VkColorComponentFlags.B;
        }

        if (mask.HasFlag(RHI.ColorWriteMask.Alpha))
        {
            result |= VkColorComponentFlags.A;
        }

        return result;
    }

    /// <summary>
    /// 根据混合模式创建颜色混合附件状态
    /// </summary>
    public static VkPipelineColorBlendAttachmentState CreateBlendAttachmentFromMode(RHI.BlendMode blendMode)
    {
        return blendMode switch
        {
            RHI.BlendMode.Alpha => new VkPipelineColorBlendAttachmentState
            {
                BlendEnable = 1,
                SrcColorBlendFactor = VkBlendFactor.SrcAlpha,
                DstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = VkBlendOp.Add,
                SrcAlphaBlendFactor = VkBlendFactor.One,
                DstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                AlphaBlendOp = VkBlendOp.Add,
                ColorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A
            },
            RHI.BlendMode.Additive => new VkPipelineColorBlendAttachmentState
            {
                BlendEnable = 1,
                SrcColorBlendFactor = VkBlendFactor.SrcAlpha,
                DstColorBlendFactor = VkBlendFactor.One,
                ColorBlendOp = VkBlendOp.Add,
                SrcAlphaBlendFactor = VkBlendFactor.One,
                DstAlphaBlendFactor = VkBlendFactor.One,
                AlphaBlendOp = VkBlendOp.Add,
                ColorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A
            },
            RHI.BlendMode.Multiply => new VkPipelineColorBlendAttachmentState
            {
                BlendEnable = 1,
                SrcColorBlendFactor = VkBlendFactor.DstColor,
                DstColorBlendFactor = VkBlendFactor.Zero,
                ColorBlendOp = VkBlendOp.Add,
                SrcAlphaBlendFactor = VkBlendFactor.DstAlpha,
                DstAlphaBlendFactor = VkBlendFactor.Zero,
                AlphaBlendOp = VkBlendOp.Add,
                ColorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A
            },
            _ => new VkPipelineColorBlendAttachmentState
            {
                BlendEnable = 0,
                SrcColorBlendFactor = VkBlendFactor.One,
                DstColorBlendFactor = VkBlendFactor.Zero,
                ColorBlendOp = VkBlendOp.Add,
                SrcAlphaBlendFactor = VkBlendFactor.One,
                DstAlphaBlendFactor = VkBlendFactor.Zero,
                AlphaBlendOp = VkBlendOp.Add,
                ColorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A
            }
        };
    }

    #endregion

    #region 渲染通道转换

    /// <summary>
    /// 将 RHI 加载操作转换为 Vulkan 加载操作
    /// </summary>
    public static VkAttachmentLoadOp ToVkLoadOp(RHI.LoadAction action)
    {
        return action switch
        {
            RHI.LoadAction.Load => VkAttachmentLoadOp.Load,
            RHI.LoadAction.Clear => VkAttachmentLoadOp.Clear,
            RHI.LoadAction.DontCare => VkAttachmentLoadOp.DontCare,
            _ => VkAttachmentLoadOp.DontCare
        };
    }

    /// <summary>
    /// 将 RHI 存储操作转换为 Vulkan 存储操作
    /// </summary>
    public static VkAttachmentStoreOp ToVkStoreOp(RHI.StoreAction action)
    {
        return action switch
        {
            RHI.StoreAction.Store => VkAttachmentStoreOp.Store,
            RHI.StoreAction.DontCare => VkAttachmentStoreOp.DontCare,
            _ => VkAttachmentStoreOp.DontCare
        };
    }

    #endregion

    #region 采样器转换

    /// <summary>
    /// 将 RHI 过滤模式转换为 Vulkan 过滤器
    /// </summary>
    public static VkFilter ToVkFilter(RHI.FilterMode filterMode)
    {
        return filterMode switch
        {
            RHI.FilterMode.Nearest => VkFilter.Nearest,
            RHI.FilterMode.Linear => VkFilter.Linear,
            _ => VkFilter.Linear
        };
    }

    /// <summary>
    /// 将 RHI 采样器寻址模式转换为 Vulkan 采样器寻址模式
    /// </summary>
    public static VkSamplerAddressMode ToVkSamplerAddressMode(RHI.SamplerAddressMode mode)
    {
        return mode switch
        {
            RHI.SamplerAddressMode.Repeat => VkSamplerAddressMode.Repeat,
            RHI.SamplerAddressMode.MirroredRepeat => VkSamplerAddressMode.MirroredRepeat,
            RHI.SamplerAddressMode.ClampToEdge => VkSamplerAddressMode.ClampToEdge,
            RHI.SamplerAddressMode.ClampToBorder => VkSamplerAddressMode.ClampToBorder,
            RHI.SamplerAddressMode.MirrorClampToEdge => VkSamplerAddressMode.MirrorClampToEdge,
            _ => VkSamplerAddressMode.Repeat
        };
    }

    #endregion

    #region 交换链转换

    /// <summary>
    /// 将 RHI 呈现模式转换为 Vulkan 呈现模式
    /// </summary>
    public static VkPresentModeKHR ToVkPresentMode(RHI.PresentMode presentMode)
    {
        return presentMode switch
        {
            RHI.PresentMode.Immediate => VkPresentModeKHR.Immediate,
            RHI.PresentMode.Mailbox => VkPresentModeKHR.Mailbox,
            RHI.PresentMode.Fifo => VkPresentModeKHR.Fifo,
            RHI.PresentMode.FifoRelaxed => VkPresentModeKHR.FifoRelaxed,
            _ => VkPresentModeKHR.Fifo
        };
    }

    #endregion

    #region 同步转换

    /// <summary>
    /// 将 RHI 管线阶段标志转换为 Vulkan 管线阶段标志
    /// </summary>
    public static VkPipelineStageFlagBits ToVkPipelineStage(RHI.PipelineStageFlag stage)
    {
        var result = (VkPipelineStageFlagBits)0;

        if (stage.HasFlag(RHI.PipelineStageFlag.TopOfPipe))
        {
            result |= VkPipelineStageFlagBits.TopOfPipe;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.DrawIndirect))
        {
            result |= VkPipelineStageFlagBits.DrawIndirect;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.VertexInput))
        {
            result |= VkPipelineStageFlagBits.VertexInput;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.VertexShader))
        {
            result |= VkPipelineStageFlagBits.VertexShader;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.FragmentShader))
        {
            result |= VkPipelineStageFlagBits.FragmentShader;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.EarlyFragmentTests))
        {
            result |= VkPipelineStageFlagBits.EarlyFragmentTests;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.LateFragmentTests))
        {
            result |= VkPipelineStageFlagBits.LateFragmentTests;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.ColorAttachmentOutput))
        {
            result |= VkPipelineStageFlagBits.ColorAttachmentOutput;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.ComputeShader))
        {
            result |= VkPipelineStageFlagBits.ComputeShader;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.Transfer))
        {
            result |= VkPipelineStageFlagBits.Transfer;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.BottomOfPipe))
        {
            result |= VkPipelineStageFlagBits.BottomOfPipe;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.Host))
        {
            result |= VkPipelineStageFlagBits.Host;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.AllGraphics))
        {
            result |= VkPipelineStageFlagBits.AllGraphics;
        }

        if (stage.HasFlag(RHI.PipelineStageFlag.AllCommands))
        {
            result |= VkPipelineStageFlagBits.AllCommands;
        }

        return result;
    }

    /// <summary>
    /// 将 RHI 访问标志转换为 Vulkan 访问标志
    /// </summary>
    public static VkAccessFlagBits ToVkAccess(RHI.AccessFlag access)
    {
        var result = (VkAccessFlagBits)0;

        if (access.HasFlag(RHI.AccessFlag.IndirectCommandRead))
        {
            result |= VkAccessFlagBits.IndirectCommandRead;
        }

        if (access.HasFlag(RHI.AccessFlag.IndexRead))
        {
            result |= VkAccessFlagBits.IndexRead;
        }

        if (access.HasFlag(RHI.AccessFlag.VertexAttributeRead))
        {
            result |= VkAccessFlagBits.VertexAttributeRead;
        }

        if (access.HasFlag(RHI.AccessFlag.UniformRead))
        {
            result |= VkAccessFlagBits.UniformRead;
        }

        if (access.HasFlag(RHI.AccessFlag.InputAttachmentRead))
        {
            result |= VkAccessFlagBits.InputAttachmentRead;
        }

        if (access.HasFlag(RHI.AccessFlag.ShaderRead))
        {
            result |= VkAccessFlagBits.ShaderRead;
        }

        if (access.HasFlag(RHI.AccessFlag.ShaderWrite))
        {
            result |= VkAccessFlagBits.ShaderWrite;
        }

        if (access.HasFlag(RHI.AccessFlag.ColorAttachmentRead))
        {
            result |= VkAccessFlagBits.ColorAttachmentRead;
        }

        if (access.HasFlag(RHI.AccessFlag.ColorAttachmentWrite))
        {
            result |= VkAccessFlagBits.ColorAttachmentWrite;
        }

        if (access.HasFlag(RHI.AccessFlag.DepthStencilAttachmentRead))
        {
            result |= VkAccessFlagBits.DepthStencilAttachmentRead;
        }

        if (access.HasFlag(RHI.AccessFlag.DepthStencilAttachmentWrite))
        {
            result |= VkAccessFlagBits.DepthStencilAttachmentWrite;
        }

        if (access.HasFlag(RHI.AccessFlag.TransferRead))
        {
            result |= VkAccessFlagBits.TransferRead;
        }

        if (access.HasFlag(RHI.AccessFlag.TransferWrite))
        {
            result |= VkAccessFlagBits.TransferWrite;
        }

        if (access.HasFlag(RHI.AccessFlag.HostRead))
        {
            result |= VkAccessFlagBits.HostRead;
        }

        if (access.HasFlag(RHI.AccessFlag.HostWrite))
        {
            result |= VkAccessFlagBits.HostWrite;
        }

        if (access.HasFlag(RHI.AccessFlag.MemoryRead))
        {
            result |= VkAccessFlagBits.MemoryRead;
        }

        if (access.HasFlag(RHI.AccessFlag.MemoryWrite))
        {
            result |= VkAccessFlagBits.MemoryWrite;
        }

        return result;
    }

    #endregion

    #region 着色器转换

    /// <summary>
    /// 将 RHI 着色器阶段转换为 Vulkan 着色器阶段标志
    /// </summary>
    public static VkShaderStageFlagBits ToVkShaderStage(ShaderStage stage)
    {
        return stage switch
        {
            ShaderStage.Vertex => VkShaderStageFlagBits.Vertex,
            ShaderStage.Fragment => VkShaderStageFlagBits.Fragment,
            ShaderStage.Geometry => VkShaderStageFlagBits.Geometry,
            ShaderStage.TessControl => VkShaderStageFlagBits.TessellationControl,
            ShaderStage.TessEvaluation => VkShaderStageFlagBits.TessellationEvaluation,
            ShaderStage.Compute => VkShaderStageFlagBits.Compute,
            _ => VkShaderStageFlagBits.Vertex
        };
    }

    /// <summary>
    /// 将 RHI 着色器阶段标志转换为 Vulkan 着色器阶段标志
    /// </summary>
    public static VkShaderStageFlagBits ToVkShaderStageFlags(RHI.ShaderStageFlag flags)
    {
        var result = (VkShaderStageFlagBits)0;

        if (flags.HasFlag(RHI.ShaderStageFlag.Vertex))
        {
            result |= VkShaderStageFlagBits.Vertex;
        }

        if (flags.HasFlag(RHI.ShaderStageFlag.TessControl))
        {
            result |= VkShaderStageFlagBits.TessellationControl;
        }

        if (flags.HasFlag(RHI.ShaderStageFlag.TessEvaluation))
        {
            result |= VkShaderStageFlagBits.TessellationEvaluation;
        }

        if (flags.HasFlag(RHI.ShaderStageFlag.Geometry))
        {
            result |= VkShaderStageFlagBits.Geometry;
        }

        if (flags.HasFlag(RHI.ShaderStageFlag.Fragment))
        {
            result |= VkShaderStageFlagBits.Fragment;
        }

        if (flags.HasFlag(RHI.ShaderStageFlag.Compute))
        {
            result |= VkShaderStageFlagBits.Compute;
        }

        return result;
    }

    /// <summary>
    /// 将 RHI 描述符类型转换为 Vulkan 描述符类型
    /// </summary>
    public static VkDescriptorType ToVkDescriptorType(RHI.DescriptorType type)
    {
        return type switch
        {
            RHI.DescriptorType.UniformBuffer => VkDescriptorType.UniformBuffer,
            RHI.DescriptorType.StorageBuffer => VkDescriptorType.StorageBuffer,
            RHI.DescriptorType.CombinedImageSampler => VkDescriptorType.CombinedImageSampler,
            RHI.DescriptorType.SampledImage => VkDescriptorType.SampledImage,
            RHI.DescriptorType.StorageImage => VkDescriptorType.StorageImage,
            RHI.DescriptorType.Sampler => VkDescriptorType.Sampler,
            RHI.DescriptorType.InputAttachment => VkDescriptorType.InputAttachment,
            _ => VkDescriptorType.UniformBuffer
        };
    }

    #endregion
}

#endregion

/// <summary>
/// Vulkan 描述符池原生 API 补充
/// </summary>
internal static unsafe class VulkanDescriptorNative
{
    private const string LibraryName = "vulkan";

    /// <summary>
    /// 创建描述符池
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern VkResult vkCreateDescriptorPool(VkDevice device, VkDescriptorPoolCreateInfo* pCreateInfo, void* pAllocator, out VkDescriptorPool pDescriptorPool);

    /// <summary>
    /// 销毁描述符池
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
    public static extern void vkDestroyDescriptorPool(VkDevice device, VkDescriptorPool descriptorPool, void* pAllocator);
}

/// <summary>
/// Vulkan 图形设备，基于 Khronos Vulkan API 实现
/// </summary>
public sealed unsafe class VulkanDevice : RHI.IDevice
{
    #region 属性

    /// <summary>
    /// Vulkan 实例句柄
    /// </summary>
    public VkInstance Instance { get; private set; }

    /// <summary>
    /// Vulkan 物理设备句柄
    /// </summary>
    public VkPhysicalDevice PhysicalDevice { get; private set; }

    /// <summary>
    /// Vulkan 逻辑设备句柄
    /// </summary>
    public VkDevice LogicalDevice { get; private set; }

    /// <summary>
    /// Vulkan 呈现队列
    /// </summary>
    public VkQueue Queue { get; private set; }

    /// <summary>
    /// Vulkan 命令池
    /// </summary>
    public VkCommandPool CommandPool { get; private set; }

    /// <summary>
    /// 是否启用验证层
    /// </summary>
    public bool EnableValidationLayers { get; }

    /// <summary>
    /// 是否启用光线追踪扩展
    /// </summary>
    public bool EnableRayTracing { get; }

    /// <summary>
    /// 图形队列族索引
    /// </summary>
    public uint QueueFamilyIndex { get; private set; }

    #endregion

    #region 内部状态

    private VkDescriptorPool _descriptorPool;
    private readonly Dictionary<ulong, VulkanResource> _resources = new();
    private bool _isDisposed;

    private static readonly string[] ValidationLayers = ["VK_LAYER_KHRONOS_validation"];
    private static readonly string[] InstanceExtensions = ["VK_KHR_surface", "VK_KHR_win32_surface"];
    private static readonly string[] DeviceExtensions = ["VK_KHR_swapchain"];

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建 Vulkan 设备
    /// </summary>
    /// <param name="enableValidationLayers">是否启用验证层</param>
    /// <param name="enableRayTracing">是否启用光线追踪</param>
    public VulkanDevice(bool enableValidationLayers = true, bool enableRayTracing = false)
    {
        EnableValidationLayers = enableValidationLayers;
        EnableRayTracing = enableRayTracing;

        CreateInstance();
        SelectPhysicalDevice();
        CreateLogicalDevice();
        CreateCommandPool();
        CreateDescriptorPool();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 创建 Vulkan 实例
    /// </summary>
    private void CreateInstance()
    {
        var appInfo = new VkApplicationInfo
        {
            SType = VkStructureType.ApplicationInfo,
            PNext = null,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Gnosis"),
            ApplicationVersion = 0,
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("Gnosis Engine"),
            EngineVersion = 0,
            ApiVersion = (1 << 22) | (3 << 12)
        };

        byte** enabledLayers = null;
        uint enabledLayerCount = 0;

        byte** ppLayerNames = null;
        if (EnableValidationLayers)
        {
            enabledLayerCount = (uint)ValidationLayers.Length;
            ppLayerNames = (byte**)Marshal.AllocHGlobal(ValidationLayers.Length * nint.Size);
            for (var i = 0; i < ValidationLayers.Length; i++)
            {
                ppLayerNames[i] = (byte*)Marshal.StringToHGlobalAnsi(ValidationLayers[i]);
            }

            enabledLayers = ppLayerNames;
        }

        byte** ppExtensionNames = (byte**)Marshal.AllocHGlobal(InstanceExtensions.Length * nint.Size);
        for (var i = 0; i < InstanceExtensions.Length; i++)
        {
            ppExtensionNames[i] = (byte*)Marshal.StringToHGlobalAnsi(InstanceExtensions[i]);
        }

        var createInfo = new VkInstanceCreateInfo
        {
            SType = VkStructureType.InstanceCreateInfo,
            PNext = null,
            Flags = 0,
            PApplicationInfo = &appInfo,
            EnabledLayerCount = enabledLayerCount,
            PpEnabledLayerNames = enabledLayers,
            EnabledExtensionCount = (uint)InstanceExtensions.Length,
            PpEnabledExtensionNames = ppExtensionNames
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateInstance(&createInfo, null, out var instance),
            "创建 Vulkan 实例");

        Instance = instance;

        Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
        Marshal.FreeHGlobal((nint)appInfo.PEngineName);

        if (ppLayerNames != null)
        {
            for (var i = 0; i < ValidationLayers.Length; i++)
            {
                Marshal.FreeHGlobal((nint)ppLayerNames[i]);
            }

            Marshal.FreeHGlobal((nint)ppLayerNames);
        }

        for (var i = 0; i < InstanceExtensions.Length; i++)
        {
            Marshal.FreeHGlobal((nint)ppExtensionNames[i]);
        }

        Marshal.FreeHGlobal((nint)ppExtensionNames);
    }

    /// <summary>
    /// 选择物理设备
    /// </summary>
    private void SelectPhysicalDevice()
    {
        uint deviceCount = 0;
        VulkanNative.vkEnumeratePhysicalDevices(Instance, &deviceCount, null);

        if (deviceCount == 0)
        {
            throw new InvalidOperationException("未找到支持 Vulkan 的 GPU");
        }

        var devices = stackalloc VkPhysicalDevice[(int)deviceCount];
        VulkanNative.vkEnumeratePhysicalDevices(Instance, &deviceCount, devices);

        VkPhysicalDevice selectedDevice = VkPhysicalDevice.Null;
        VkPhysicalDeviceType bestType = VkPhysicalDeviceType.Other;

        for (var i = 0; i < (int)deviceCount; i++)
        {
            var propsBuffer = stackalloc byte[832];
            VulkanNative.vkGetPhysicalDeviceProperties(devices[i], propsBuffer);
            var deviceType = *(VkPhysicalDeviceType*)(propsBuffer + 16);

            if (deviceType == VkPhysicalDeviceType.DiscreteGpu)
            {
                selectedDevice = devices[i];
                bestType = deviceType;
                break;
            }

            if (bestType != VkPhysicalDeviceType.DiscreteGpu && deviceType == VkPhysicalDeviceType.IntegratedGpu)
            {
                selectedDevice = devices[i];
                bestType = deviceType;
            }
            else if (selectedDevice.IsNull)
            {
                selectedDevice = devices[i];
            }
        }

        if (selectedDevice.IsNull)
        {
            throw new InvalidOperationException("未找到合适的物理设备");
        }

        PhysicalDevice = selectedDevice;
        QueueFamilyIndex = FindQueueFamily(PhysicalDevice);
    }

    /// <summary>
    /// 创建逻辑设备
    /// </summary>
    private void CreateLogicalDevice()
    {
        var queuePriority = 1.0f;

        var queueCreateInfo = new VkDeviceQueueCreateInfo
        {
            SType = VkStructureType.DeviceQueueCreateInfo,
            PNext = null,
            Flags = 0,
            QueueFamilyIndex = QueueFamilyIndex,
            QueueCount = 1,
            PQueuePriorities = &queuePriority
        };

        byte** ppExtensionNames = (byte**)Marshal.AllocHGlobal(DeviceExtensions.Length * nint.Size);
        for (var i = 0; i < DeviceExtensions.Length; i++)
        {
            ppExtensionNames[i] = (byte*)Marshal.StringToHGlobalAnsi(DeviceExtensions[i]);
        }

        var createInfo = new VkDeviceCreateInfo
        {
            SType = VkStructureType.DeviceCreateInfo,
            PNext = null,
            Flags = 0,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreateInfo,
            EnabledLayerCount = 0,
            PpEnabledLayerNames = null,
            EnabledExtensionCount = (uint)DeviceExtensions.Length,
            PpEnabledExtensionNames = ppExtensionNames,
            PEnabledFeatures = null
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateDevice(PhysicalDevice, &createInfo, null, out var device),
            "创建逻辑设备");

        LogicalDevice = device;

        VulkanNative.vkGetDeviceQueue(LogicalDevice, QueueFamilyIndex, 0, out var queue);
        Queue = queue;

        for (var i = 0; i < DeviceExtensions.Length; i++)
        {
            Marshal.FreeHGlobal((nint)ppExtensionNames[i]);
        }

        Marshal.FreeHGlobal((nint)ppExtensionNames);
    }

    /// <summary>
    /// 创建命令池
    /// </summary>
    private void CreateCommandPool()
    {
        var createInfo = new VkCommandPoolCreateInfo
        {
            SType = VkStructureType.CommandPoolCreateInfo,
            PNext = null,
            Flags = VkCommandPoolCreateFlagBits.ResetCommandBuffer,
            QueueFamilyIndex = QueueFamilyIndex
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateCommandPool(LogicalDevice, &createInfo, null, out var commandPool),
            "创建命令池");

        CommandPool = commandPool;
    }

    /// <summary>
    /// 创建描述符池
    /// </summary>
    private void CreateDescriptorPool()
    {
        var poolSizes = new VkDescriptorPoolSize[]
        {
            new() { Type = VkDescriptorType.UniformBuffer, DescriptorCount = 1024 },
            new() { Type = VkDescriptorType.CombinedImageSampler, DescriptorCount = 1024 },
            new() { Type = VkDescriptorType.Sampler, DescriptorCount = 256 },
            new() { Type = VkDescriptorType.StorageBuffer, DescriptorCount = 256 }
        };

        fixed (VkDescriptorPoolSize* pPoolSizes = poolSizes)
        {
            var createInfo = new VkDescriptorPoolCreateInfo
            {
                SType = (VkStructureType)51,
                PNext = null,
                Flags = 1,
                MaxSets = 1024,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = pPoolSizes
            };

            VulkanNative.CheckResult(
                VulkanDescriptorNative.vkCreateDescriptorPool(LogicalDevice, &createInfo, null, out _descriptorPool),
                "创建描述符池");
        }
    }

    #endregion

    #region IDevice 实现

    /// <summary>
    /// 创建缓冲区资源
    /// </summary>
    /// <param name="desc">缓冲区描述</param>
    /// <returns>缓冲区资源</returns>
    public RHI.IResource CreateBuffer(in RHI.BufferDesc desc)
    {
        var usage = VulkanConversions.ToVkBufferUsage(desc.Usage);

        if (desc.HostVisible)
        {
            usage |= VkBufferUsageFlagBits.TransferDst;
        }

        var createInfo = new VkBufferCreateInfo
        {
            SType = VkStructureType.BufferCreateInfo,
            PNext = null,
            Flags = 0,
            Size = desc.Size,
            Usage = usage,
            SharingMode = VkSharingMode.Exclusive,
            QueueFamilyIndexCount = 0,
            PQueueFamilyIndices = null
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateBuffer(LogicalDevice, &createInfo, null, out var buffer),
            "创建缓冲区");

        var memReqs = new VkMemoryRequirements();
        VulkanNative.vkGetBufferMemoryRequirements(LogicalDevice, buffer, &memReqs);

        var memoryProperties = desc.HostVisible
            ? VkMemoryPropertyFlagBits.HostVisible | VkMemoryPropertyFlagBits.HostCoherent
            : VkMemoryPropertyFlagBits.DeviceLocal;

        var memoryTypeIndex = FindMemoryType(memReqs.MemoryTypeBits, memoryProperties);

        var allocInfo = new VkMemoryAllocateInfo
        {
            SType = VkStructureType.MemoryAllocateInfo,
            PNext = null,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = memoryTypeIndex
        };

        VulkanNative.CheckResult(
            VulkanNative.vkAllocateMemory(LogicalDevice, &allocInfo, null, out var memory),
            "分配缓冲区内存");

        VulkanNative.CheckResult(
            VulkanNative.vkBindBufferMemory(LogicalDevice, buffer, memory, 0),
            "绑定缓冲区内存");

        if (desc.InitialData != null && desc.InitialData.Length > 0)
        {
            VulkanNative.CheckResult(
                VulkanNative.vkMapMemory(LogicalDevice, memory, 0, desc.Size, 0, out var pData),
                "映射缓冲区内存");

            if (pData != null)
            {
                fixed (byte* pSrc = desc.InitialData)
                {
                    Buffer.MemoryCopy(pSrc, pData, (long)desc.Size, desc.InitialData.Length);
                }

                VulkanNative.vkUnmapMemory(LogicalDevice, memory);
            }
        }

        var resource = new VulkanResource(LogicalDevice, buffer, memory, memReqs.Size, RHI.ResourceFormat.Unknown);
        _resources[resource.Id] = resource;

        return resource;
    }

    /// <summary>
    /// 创建纹理资源
    /// </summary>
    /// <param name="desc">纹理描述</param>
    /// <returns>纹理资源</returns>
    public RHI.IResource CreateTexture(in RHI.TextureDesc desc)
    {
        var usage = VulkanConversions.ToVkImageUsage(desc.Usage);
        var format = VulkanConversions.ToVkFormat(desc.Format);
        var imageType = VulkanConversions.ToVkImageType(desc.Dimension);
        var resourceType = VulkanConversions.ToResourceType(desc.Dimension);

        if (VulkanConversions.IsDepthFormat(desc.Format))
        {
            usage |= VkImageUsageFlagBits.DepthStencilAttachment;
        }

        var createInfo = new VkImageCreateInfo
        {
            SType = VkStructureType.ImageCreateInfo,
            PNext = null,
            Flags = 0,
            ImageType = imageType,
            Format = format,
            Extent = new VkExtent3D { Width = desc.Width, Height = desc.Height, Depth = desc.Depth },
            MipLevels = desc.MipLevels,
            ArrayLayers = desc.ArrayLayers,
            Samples = VulkanConversions.ToVkSampleCount(desc.SampleCount),
            Tiling = VkImageTiling.Optimal,
            Usage = usage,
            SharingMode = VkSharingMode.Exclusive,
            QueueFamilyIndexCount = 0,
            PQueueFamilyIndices = null,
            InitialLayout = VkImageLayout.Undefined
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateImage(LogicalDevice, &createInfo, null, out var image),
            "创建图像");

        var memReqs = new VkMemoryRequirements();
        VulkanNative.vkGetImageMemoryRequirements(LogicalDevice, image, &memReqs);

        var memoryTypeIndex = FindMemoryType(memReqs.MemoryTypeBits, VkMemoryPropertyFlagBits.DeviceLocal);

        var allocInfo = new VkMemoryAllocateInfo
        {
            SType = VkStructureType.MemoryAllocateInfo,
            PNext = null,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = memoryTypeIndex
        };

        VulkanNative.CheckResult(
            VulkanNative.vkAllocateMemory(LogicalDevice, &allocInfo, null, out var memory),
            "分配图像内存");

        VulkanNative.CheckResult(
            VulkanNative.vkBindImageMemory(LogicalDevice, image, memory, 0),
            "绑定图像内存");

        var aspectMask = VulkanConversions.IsDepthFormat(desc.Format)
            ? VkImageAspectFlagBits.Depth | VkImageAspectFlagBits.Stencil
            : VkImageAspectFlagBits.Color;

        var viewCreateInfo = new VkImageViewCreateInfo
        {
            SType = VkStructureType.ImageViewCreateInfo,
            PNext = null,
            Image = image,
            ViewType = VulkanConversions.ToVkImageViewType(desc.Dimension),
            Format = format,
            Components = new VkComponentMapping
            {
                R = VkComponentSwizzle.Identity,
                G = VkComponentSwizzle.Identity,
                B = VkComponentSwizzle.Identity,
                A = VkComponentSwizzle.Identity
            },
            SubresourceRange = new VkImageSubresourceRange
            {
                AspectMask = aspectMask,
                BaseMipLevel = 0,
                LevelCount = desc.MipLevels,
                BaseArrayLayer = 0,
                LayerCount = desc.ArrayLayers
            }
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateImageView(LogicalDevice, &viewCreateInfo, null, out var imageView),
            "创建图像视图");

        var resource = new VulkanResource(LogicalDevice, image, imageView, memory, memReqs.Size, desc.Format, resourceType);
        _resources[resource.Id] = resource;

        return resource;
    }

    /// <summary>
    /// 创建采样器
    /// </summary>
    /// <param name="desc">采样器描述</param>
    /// <returns>采样器资源</returns>
    public RHI.IResource CreateSampler(in RHI.SamplerDesc desc)
    {
        var createInfo = new VkSamplerCreateInfo
        {
            SType = (VkStructureType)40,
            PNext = null,
            Flags = 0,
            MagFilter = VulkanConversions.ToVkFilter(desc.MagFilter),
            MinFilter = VulkanConversions.ToVkFilter(desc.MinFilter),
            MipmapMode = desc.MagFilter == RHI.FilterMode.Nearest ? VkSamplerMipmapMode.Nearest : VkSamplerMipmapMode.Linear,
            AddressModeU = VulkanConversions.ToVkSamplerAddressMode(desc.AddressModeU),
            AddressModeV = VulkanConversions.ToVkSamplerAddressMode(desc.AddressModeV),
            AddressModeW = VulkanConversions.ToVkSamplerAddressMode(desc.AddressModeW),
            MipLodBias = 0.0f,
            AnisotropyEnable = desc.MaxAnisotropy > 1.0f ? 1u : 0u,
            MaxAnisotropy = desc.MaxAnisotropy,
            CompareEnable = desc.CompareEnable ? 1u : 0u,
            CompareOp = VulkanConversions.ToVkCompareOp(desc.CompareOp),
            MinLod = desc.MinLod,
            MaxLod = desc.MaxLod,
            BorderColor = VkBorderColor.FloatOpaqueBlack,
            UnnormalizedCoordinates = 0
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateSampler(LogicalDevice, &createInfo, null, out var sampler),
            "创建采样器");

        var resource = new VulkanResource(LogicalDevice, sampler);
        _resources[resource.Id] = resource;

        return resource;
    }

    /// <summary>
    /// 创建着色器模块
    /// </summary>
    /// <param name="desc">着色器描述</param>
    /// <returns>着色器资源</returns>
    public RHI.IResource CreateShader(in RHI.ShaderDesc desc)
    {
        fixed (byte* pCode = desc.Bytecode)
        {
            var createInfo = new VkShaderModuleCreateInfo
            {
                SType = VkStructureType.ShaderModuleCreateInfo,
                PNext = null,
                Flags = 0,
                CodeSize = (uint)desc.Bytecode.Length,
                PCode = (uint*)pCode
            };

            VulkanNative.CheckResult(
                VulkanNative.vkCreateShaderModule(LogicalDevice, &createInfo, null, out var shaderModule),
                "创建着色器模块");

            var resource = new VulkanResource(LogicalDevice, shaderModule, (ulong)desc.Bytecode.Length, desc.Stage, desc.EntryPoint);
            _resources[resource.Id] = resource;

            return resource;
        }
    }

    /// <summary>
    /// 创建管线状态对象
    /// </summary>
    /// <param name="desc">管线状态描述</param>
    /// <returns>管线状态对象</returns>
    public RHI.IPipelineState CreatePipelineState(in RHI.PipelineStateDesc desc)
    {
        return new VulkanPipelineState(this, in desc);
    }

    /// <summary>
    /// 创建渲染通道
    /// </summary>
    /// <param name="desc">渲染通道描述</param>
    /// <returns>渲染通道对象</returns>
    public RHI.IRhiRenderPass CreateRenderPass(in RHI.RenderPassDesc desc)
    {
        var attachments = stackalloc VkAttachmentDescription[desc.Attachments.Length];
        for (var i = 0; i < desc.Attachments.Length; i++)
        {
            var att = desc.Attachments[i];
            var isDepth = VulkanConversions.IsDepthFormat(att.Format);

            attachments[i] = new VkAttachmentDescription
            {
                Flags = 0,
                Format = VulkanConversions.ToVkFormat(att.Format),
                Samples = VulkanConversions.ToVkSampleCount(att.SampleCount),
                LoadOp = VulkanConversions.ToVkLoadOp(att.LoadAction),
                StoreOp = VulkanConversions.ToVkStoreOp(att.StoreAction),
                StencilLoadOp = VulkanConversions.ToVkLoadOp(att.StencilLoadAction),
                StencilStoreOp = VulkanConversions.ToVkStoreOp(att.StencilStoreAction),
                InitialLayout = isDepth ? VkImageLayout.Undefined : VkImageLayout.Undefined,
                FinalLayout = isDepth ? VkImageLayout.DepthStencilAttachmentOptimal : VkImageLayout.PresentSrcKHR
            };
        }

        var colorRefs = stackalloc VkAttachmentReference[16];
        var depthRef = stackalloc VkAttachmentReference[1];
        var inputRefs = stackalloc VkAttachmentReference[16];
        uint colorCount = 0;

        var subpasses = stackalloc VkSubpassDescription[desc.SubPasses.Length];
        for (var i = 0; i < desc.SubPasses.Length; i++)
        {
            var sub = desc.SubPasses[i];

            colorCount = 0;
            for (var j = 0; j < sub.ColorAttachments.Length && j < 16; j++)
            {
                colorRefs[colorCount] = new VkAttachmentReference
                {
                    Attachment = sub.ColorAttachments[j],
                    Layout = VkImageLayout.ColorAttachmentOptimal
                };
                colorCount++;
            }

            VkAttachmentReference* pDepthRef = null;
            if (sub.DepthStencilAttachment != uint.MaxValue)
            {
                depthRef[0] = new VkAttachmentReference
                {
                    Attachment = sub.DepthStencilAttachment,
                    Layout = VkImageLayout.DepthStencilAttachmentOptimal
                };
                pDepthRef = depthRef;
            }

            uint inputCount = 0;
            for (var j = 0; j < sub.InputAttachments.Length && j < 16; j++)
            {
                inputRefs[inputCount] = new VkAttachmentReference
                {
                    Attachment = sub.InputAttachments[j],
                    Layout = VkImageLayout.ShaderReadOnlyOptimal
                };
                inputCount++;
            }

            subpasses[i] = new VkSubpassDescription
            {
                Flags = 0,
                PipelineBindPoint = VkPipelineBindPoint.Graphics,
                InputAttachmentCount = inputCount,
                PInputAttachments = inputRefs,
                ColorAttachmentCount = colorCount,
                PColorAttachments = colorRefs,
                PResolveAttachments = null,
                PDepthStencilAttachment = pDepthRef,
                PreserveAttachmentCount = 0,
                PPreserveAttachments = null
            };
        }

        var dependencies = stackalloc VkSubpassDependency[desc.Dependencies.Length];
        for (var i = 0; i < desc.Dependencies.Length; i++)
        {
            var dep = desc.Dependencies[i];
            dependencies[i] = new VkSubpassDependency
            {
                SrcSubpass = dep.SrcSubPass,
                DstSubpass = dep.DstSubPass,
                SrcStageMask = VulkanConversions.ToVkPipelineStage(dep.SrcStage),
                DstStageMask = VulkanConversions.ToVkPipelineStage(dep.DstStage),
                SrcAccessMask = VulkanConversions.ToVkAccess(dep.SrcAccess),
                DstAccessMask = VulkanConversions.ToVkAccess(dep.DstAccess),
                DependencyFlags = 0
            };
        }

        var createInfo = new VkRenderPassCreateInfo
        {
            SType = VkStructureType.RenderPassCreateInfo,
            PNext = null,
            Flags = 0,
            AttachmentCount = (uint)desc.Attachments.Length,
            PAttachments = attachments,
            SubpassCount = (uint)desc.SubPasses.Length,
            PSubpasses = subpasses,
            DependencyCount = (uint)desc.Dependencies.Length,
            PDependencies = dependencies
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateRenderPass(LogicalDevice, &createInfo, null, out var renderPass),
            "创建渲染通道");

        return new VulkanRenderPass(LogicalDevice, renderPass, (uint)desc.Attachments.Length, (uint)desc.SubPasses.Length);
    }

    /// <summary>
    /// 创建帧缓冲
    /// </summary>
    /// <param name="desc">帧缓冲描述</param>
    /// <returns>帧缓冲对象</returns>
    public RHI.IRhiFramebuffer CreateFramebuffer(in RHI.FramebufferDesc desc)
    {
        var vkRenderPass = ((VulkanRenderPass)desc.RenderPass).Handle;

        var attachmentViews = stackalloc VkImageView[desc.Attachments.Length];
        for (var i = 0; i < desc.Attachments.Length; i++)
        {
            var vkResource = (VulkanResource)desc.Attachments[i];
            attachmentViews[i] = vkResource.ImageViewHandle;
        }

        var createInfo = new VkFramebufferCreateInfo
        {
            SType = VkStructureType.FramebufferCreateInfo,
            PNext = null,
            Flags = 0,
            RenderPass = vkRenderPass,
            AttachmentCount = (uint)desc.Attachments.Length,
            PAttachments = attachmentViews,
            Width = desc.Width,
            Height = desc.Height,
            Layers = desc.Layers
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateFramebuffer(LogicalDevice, &createInfo, null, out var framebuffer),
            "创建帧缓冲");

        return new VulkanFramebuffer(LogicalDevice, framebuffer, desc.Width, desc.Height, desc.Attachments, desc.RenderPass);
    }

    /// <summary>
    /// 创建交换链
    /// </summary>
    /// <param name="desc">交换链描述</param>
    /// <returns>交换链对象</returns>
    public RHI.IRhiSwapchain CreateSwapchain(in RHI.SwapchainDesc desc)
    {
        return new VulkanSwapchain(Instance, PhysicalDevice, LogicalDevice, Queue, QueueFamilyIndex, in desc);
    }

    /// <summary>
    /// 创建围栏
    /// </summary>
    /// <param name="signaled">是否创建为已触发状态</param>
    /// <returns>围栏对象</returns>
    public RHI.IRhiFence CreateFence(bool signaled = false)
    {
        var createInfo = new VkFenceCreateInfo
        {
            SType = VkStructureType.FenceCreateInfo,
            PNext = null,
            Flags = signaled ? 1u : 0u
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateFence(LogicalDevice, &createInfo, null, out var fence),
            "创建栅栏");

        return new VulkanFence(LogicalDevice, fence);
    }

    /// <summary>
    /// 创建信号量
    /// </summary>
    /// <returns>信号量对象</returns>
    public RHI.IRhiSemaphore CreateSemaphore()
    {
        var createInfo = new VkSemaphoreCreateInfo
        {
            SType = VkStructureType.SemaphoreCreateInfo,
            PNext = null,
            Flags = 0
        };

        VulkanNative.CheckResult(
            VulkanNative.vkCreateSemaphore(LogicalDevice, &createInfo, null, out var semaphore),
            "创建信号量");

        return new VulkanSemaphore(LogicalDevice, semaphore);
    }

    /// <summary>
    /// 创建命令表
    /// </summary>
    /// <returns>命令表对象</returns>
    public RHI.ICommandTable CreateCommandTable()
    {
        var allocInfo = new VkCommandBufferAllocateInfo
        {
            SType = VkStructureType.CommandBufferAllocateInfo,
            PNext = null,
            CommandPool = CommandPool,
            Level = VkCommandBufferLevel.Primary,
            CommandBufferCount = 1
        };

        VulkanNative.CheckResult(
            VulkanNative.vkAllocateCommandBuffers(LogicalDevice, &allocInfo, out var commandBuffer),
            "分配命令缓冲区");

        return new VulkanCommandTable(LogicalDevice, CommandPool, commandBuffer);
    }

    /// <summary>
    /// 创建描述符集
    /// </summary>
    /// <param name="bindings">描述符绑定列表</param>
    /// <returns>描述符集对象</returns>
    public RHI.IRhiDescriptorSet CreateDescriptorSet(RHI.DescriptorSetBinding[] bindings)
    {
        var vkBindings = new VkDescriptorSetLayoutBinding[bindings.Length];

        for (var i = 0; i < bindings.Length; i++)
        {
            vkBindings[i] = new VkDescriptorSetLayoutBinding
            {
                Binding = bindings[i].Binding,
                DescriptorType = VulkanConversions.ToVkDescriptorType(bindings[i].DescriptorType),
                DescriptorCount = bindings[i].DescriptorCount,
                StageFlags = VulkanConversions.ToVkShaderStageFlags(bindings[i].StageFlags),
                PImmutableSamplers = null
            };
        }

        return CreateDescriptorSet(vkBindings);
    }

    /// <summary>
    /// 提交命令表到 GPU 执行
    /// </summary>
    /// <param name="commandTable">待执行的命令表</param>
    /// <param name="signalFence">执行完成后触发的围栏</param>
    public void Submit(RHI.ICommandTable commandTable, RHI.IRhiFence? signalFence = null)
    {
        var vkCommandTable = (VulkanCommandTable)commandTable;
        var cmdBuf = vkCommandTable.CommandBuffer;

        var submitInfo = new VkSubmitInfo
        {
            SType = VkStructureType.SubmitInfo,
            PNext = null,
            WaitSemaphoreCount = 0,
            PWaitSemaphores = null,
            PWaitDstStageMask = null,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuf,
            SignalSemaphoreCount = 0,
            PSignalSemaphores = null
        };

        var fenceHandle = signalFence != null ? ((VulkanFence)signalFence).Handle : VkFence.Null;

        VulkanNative.CheckResult(
            VulkanNative.vkQueueSubmit(Queue, 1, &submitInfo, fenceHandle),
            "提交命令缓冲区");
    }

    /// <summary>
    /// 等待设备空闲
    /// </summary>
    public void WaitIdle()
    {
        VulkanNative.CheckResult(
            VulkanNative.vkDeviceWaitIdle(LogicalDevice),
            "等待设备空闲");
    }

    #endregion

    #region 内部方法

    /// <summary>
    /// 根据资源 ID 获取 Vulkan 资源
    /// </summary>
    /// <param name="id">资源 ID</param>
    /// <returns>Vulkan 资源，未找到时返回 null</returns>
    internal VulkanResource? GetResource(ulong id)
    {
        return _resources.GetValueOrDefault(id);
    }

    /// <summary>
    /// 创建描述符集
    /// </summary>
    /// <param name="bindings">描述符集布局绑定数组</param>
    /// <returns>描述符集对象</returns>
    internal VulkanDescriptorSet CreateDescriptorSet(VkDescriptorSetLayoutBinding[] bindings)
    {
        fixed (VkDescriptorSetLayoutBinding* pBindings = bindings)
        {
            var layoutCreateInfo = new VkDescriptorSetLayoutCreateInfo
            {
                SType = VkStructureType.DescriptorSetLayoutCreateInfo,
                PNext = null,
                Flags = 0,
                BindingCount = (uint)bindings.Length,
                PBindings = pBindings
            };

            VulkanNative.CheckResult(
                VulkanNative.vkCreateDescriptorSetLayout(LogicalDevice, &layoutCreateInfo, null, out var layout),
                "创建描述符集布局");

            var allocInfo = new VkDescriptorSetAllocateInfo
            {
                SType = (VkStructureType)52,
                PNext = null,
                DescriptorPool = _descriptorPool,
                DescriptorSetCount = 1,
                PSetLayouts = &layout
            };

            VulkanNative.CheckResult(
                VulkanNative.vkAllocateDescriptorSets(LogicalDevice, &allocInfo, out var descriptorSet),
                "分配描述符集");

            return new VulkanDescriptorSet(LogicalDevice, _descriptorPool, descriptorSet, layout);
        }
    }

    /// <summary>
    /// 查找支持图形操作的队列族
    /// </summary>
    private static uint FindQueueFamily(VkPhysicalDevice physicalDevice)
    {
        uint queueFamilyCount = 0;
        VulkanNative.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, null);

        var propsBuffer = stackalloc byte[(int)queueFamilyCount * 24];
        VulkanNative.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, (void*)propsBuffer);

        for (uint i = 0; i < queueFamilyCount; i++)
        {
            var queueFlags = *(VkQueueFlagBits*)(propsBuffer + (int)i * 24);
            if (queueFlags.HasFlag(VkQueueFlagBits.Graphics))
            {
                return i;
            }
        }

        throw new InvalidOperationException("未找到支持图形操作的队列族");
    }

    /// <summary>
    /// 查找合适的内存类型
    /// </summary>
    private uint FindMemoryType(uint memoryTypeBits, VkMemoryPropertyFlagBits requiredProperties)
    {
        var memPropsBuffer = stackalloc byte[520];
        VulkanNative.vkGetPhysicalDeviceMemoryProperties(PhysicalDevice, (void*)memPropsBuffer);

        var memoryTypeCount = *(uint*)memPropsBuffer;

        for (uint i = 0; i < memoryTypeCount; i++)
        {
            var typeBit = 1u << (int)i;
            if ((memoryTypeBits & typeBit) == 0)
            {
                continue;
            }

            var propertyFlags = *(uint*)(memPropsBuffer + 4 + (int)i * 8);
            if (((VkMemoryPropertyFlagBits)propertyFlags).HasFlag(requiredProperties))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"未找到合适的内存类型（需求：{requiredProperties}）");
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// 释放 Vulkan 设备及所有关联资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        VulkanNative.vkDeviceWaitIdle(LogicalDevice);

        foreach (var resource in _resources.Values)
        {
            resource.Dispose();
        }

        _resources.Clear();

        if (!_descriptorPool.IsNull)
        {
            VulkanDescriptorNative.vkDestroyDescriptorPool(LogicalDevice, _descriptorPool, null);
        }

        if (!CommandPool.IsNull)
        {
            VulkanNative.vkDestroyCommandPool(LogicalDevice, CommandPool, null);
        }

        if (!LogicalDevice.IsNull)
        {
            VulkanNative.vkDestroyDevice(LogicalDevice, null);
        }

        if (!Instance.IsNull)
        {
            VulkanNative.vkDestroyInstance(Instance, null);
        }
    }

    #endregion
}

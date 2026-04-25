using System.Runtime.InteropServices;

namespace Gnosis.Graphic.RHI.Vulkan;

#region 基础结构

/// <summary>
/// 二维范围
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkExtent2D
{
    public uint Width;
    public uint Height;
}

/// <summary>
/// 三维范围
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkExtent3D
{
    public uint Width;
    public uint Height;
    public uint Depth;
}

/// <summary>
/// 二维偏移
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkOffset2D
{
    public int X;
    public int Y;
}

/// <summary>
/// 二维矩形
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkRect2D
{
    public VkOffset2D Offset;
    public VkExtent2D Extent;
}

/// <summary>
/// 视口
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkViewport
{
    public float X;
    public float Y;
    public float Width;
    public float Height;
    public float MinDepth;
    public float MaxDepth;
}

/// <summary>
/// 描述符池大小
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkDescriptorPoolSize
{
    public VkDescriptorType Type;
    public uint DescriptorCount;
}

/// <summary>
/// 描述符池创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDescriptorPoolCreateInfo
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
public unsafe struct VkDescriptorSetAllocateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public VkDescriptorPool DescriptorPool;
    public uint DescriptorSetCount;
    public VkDescriptorSetLayout* PSetLayouts;
}

/// <summary>
/// 管线动态状态创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineDynamicStateCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint DynamicStateCount;
    public VkDynamicState* PDynamicStates;
}

/// <summary>
/// 动态状态枚举
/// </summary>
public enum VkDynamicState
{
    Viewport = 0,
    Scissor = 1,
    LineWidth = 2,
    DepthBias = 3,
    BlendConstants = 4,
    DepthBounds = 5,
    StencilCompareMask = 6,
    StencilWriteMask = 7,
    StencilReference = 8
}

/// <summary>
/// 缓冲区到图像复制区域
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkBufferImageCopy
{
    public ulong BufferOffset;
    public uint BufferRowLength;
    public uint BufferImageHeight;
    public VkImageSubresourceLayers ImageSubresource;
    public VkOffset3D ImageOffset;
    public VkExtent3D ImageExtent;
}

/// <summary>
/// 图像到图像复制区域
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkImageCopy
{
    public VkImageSubresourceLayers SrcSubresource;
    public VkOffset3D SrcOffset;
    public VkImageSubresourceLayers DstSubresource;
    public VkOffset3D DstOffset;
    public VkExtent3D Extent;
}

/// <summary>
/// 图像子资源层
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkImageSubresourceLayers
{
    public VkImageAspectFlagBits AspectMask;
    public uint MipLevel;
    public uint BaseArrayLayer;
    public uint LayerCount;
}

/// <summary>
/// 三维偏移
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkOffset3D
{
    public int X;
    public int Y;
    public int Z;
}

#endregion

#region 清除值

/// <summary>
/// 清除颜色值
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public unsafe struct VkClearColorValue
{
    [FieldOffset(0)]
    public fixed float Float32[4];

    [FieldOffset(0)]
    public fixed int Int32[4];

    [FieldOffset(0)]
    public fixed uint UInt32[4];
}

/// <summary>
/// 清除深度模板值
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkClearDepthStencilValue
{
    public float Depth;
    public uint Stencil;
}

/// <summary>
/// 清除值联合体
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct VkClearValue
{
    [FieldOffset(0)]
    public VkClearColorValue Color;

    [FieldOffset(0)]
    public VkClearDepthStencilValue DepthStencil;
}

#endregion

#region 组件映射

/// <summary>
/// 分量映射
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkComponentMapping
{
    public VkComponentSwizzle R;
    public VkComponentSwizzle G;
    public VkComponentSwizzle B;
    public VkComponentSwizzle A;
}

/// <summary>
/// 图像子资源范围
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkImageSubresourceRange
{
    public VkImageAspectFlagBits AspectMask;
    public uint BaseMipLevel;
    public uint LevelCount;
    public uint BaseArrayLayer;
    public uint LayerCount;
}

#endregion

#region 实例与设备创建

/// <summary>
/// 应用程序信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkApplicationInfo
{
    public VkStructureType SType;
    public void* PNext;
    public byte* PApplicationName;
    public uint ApplicationVersion;
    public byte* PEngineName;
    public uint EngineVersion;
    public uint ApiVersion;
}

/// <summary>
/// 实例创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkInstanceCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public VkApplicationInfo* PApplicationInfo;
    public uint EnabledLayerCount;
    public byte** PpEnabledLayerNames;
    public uint EnabledExtensionCount;
    public byte** PpEnabledExtensionNames;
}

/// <summary>
/// 设备队列创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDeviceQueueCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint QueueFamilyIndex;
    public uint QueueCount;
    public float* PQueuePriorities;
}

/// <summary>
/// 设备创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDeviceCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint QueueCreateInfoCount;
    public VkDeviceQueueCreateInfo* PQueueCreateInfos;
    public uint EnabledLayerCount;
    public byte** PpEnabledLayerNames;
    public uint EnabledExtensionCount;
    public byte** PpEnabledExtensionNames;
    public void* PEnabledFeatures;
}

#endregion

#region 提交与同步

/// <summary>
/// 提交信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkSubmitInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint WaitSemaphoreCount;
    public VkSemaphore* PWaitSemaphores;
    public VkPipelineStageFlagBits* PWaitDstStageMask;
    public uint CommandBufferCount;
    public VkCommandBuffer* PCommandBuffers;
    public uint SignalSemaphoreCount;
    public VkSemaphore* PSignalSemaphores;
}

/// <summary>
/// 信号量创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkSemaphoreCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
}

/// <summary>
/// 栅栏创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkFenceCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
}

#endregion

#region 内存管理

/// <summary>
/// 内存分配信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkMemoryAllocateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public ulong AllocationSize;
    public uint MemoryTypeIndex;
}

#endregion

#region 缓冲区与图像

/// <summary>
/// 缓冲区创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkBufferCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public ulong Size;
    public VkBufferUsageFlagBits Usage;
    public VkSharingMode SharingMode;
    public uint QueueFamilyIndexCount;
    public uint* PQueueFamilyIndices;
}

/// <summary>
/// 图像创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkImageCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public VkImageType ImageType;
    public VkFormat Format;
    public VkExtent3D Extent;
    public uint MipLevels;
    public uint ArrayLayers;
    public VkSampleCountFlagBits Samples;
    public VkImageTiling Tiling;
    public VkImageUsageFlagBits Usage;
    public VkSharingMode SharingMode;
    public uint QueueFamilyIndexCount;
    public uint* PQueueFamilyIndices;
    public VkImageLayout InitialLayout;
}

/// <summary>
/// 图像视图创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkImageViewCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public VkImage Image;
    public VkImageViewType ViewType;
    public VkFormat Format;
    public VkComponentMapping Components;
    public VkImageSubresourceRange SubresourceRange;
}

#endregion

#region 着色器

/// <summary>
/// 着色器模块创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkShaderModuleCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public ulong CodeSize;
    public uint* PCode;
}

/// <summary>
/// 管线着色器阶段创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineShaderStageCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public VkShaderStageFlagBits Stage;
    public VkShaderModule Module;
    public byte* PName;
    public void* PSpecializationInfo;
}

#endregion

#region 管线状态

/// <summary>
/// 管线顶点输入状态创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineVertexInputStateCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint VertexBindingDescriptionCount;
    public void* PVertexBindingDescriptions;
    public uint VertexAttributeDescriptionCount;
    public void* PVertexAttributeDescriptions;
}

/// <summary>
/// 管线输入装配状态创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineInputAssemblyStateCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public VkPrimitiveTopology Topology;
    public uint PrimitiveRestartEnable;
}

/// <summary>
/// 管线视口状态创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineViewportStateCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint ViewportCount;
    public VkViewport* PViewports;
    public uint ScissorCount;
    public VkRect2D* PScissors;
}

/// <summary>
/// 管线光栅化状态创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineRasterizationStateCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint DepthClampEnable;
    public uint RasterizerDiscardEnable;
    public VkPolygonMode PolygonMode;
    public VkCullModeFlags CullMode;
    public VkFrontFace FrontFace;
    public uint DepthBiasEnable;
    public float DepthBiasConstantFactor;
    public float DepthBiasClamp;
    public float DepthBiasSlopeFactor;
    public float LineWidth;
}

/// <summary>
/// 管线多重采样状态创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineMultisampleStateCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public VkSampleCountFlagBits RasterizationSamples;
    public uint SampleShadingEnable;
    public float MinSampleShading;
    public void* PSampleMask;
    public uint AlphaToCoverageEnable;
    public uint AlphaToOneEnable;
}

/// <summary>
/// 模板操作状态
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkStencilOpState
{
    public VkStencilOp FailOp;
    public VkStencilOp PassOp;
    public VkStencilOp DepthFailOp;
    public VkCompareOp CompareOp;
    public uint CompareMask;
    public uint WriteMask;
    public uint Reference;
}

/// <summary>
/// 管线深度模板状态创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineDepthStencilStateCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint DepthTestEnable;
    public uint DepthWriteEnable;
    public VkCompareOp DepthCompareOp;
    public uint DepthBoundsTestEnable;
    public uint StencilTestEnable;
    public VkStencilOpState Front;
    public VkStencilOpState Back;
    public float MinDepthBounds;
    public float MaxDepthBounds;
}

/// <summary>
/// 管线颜色混合附件状态
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkPipelineColorBlendAttachmentState
{
    public uint BlendEnable;
    public VkBlendFactor SrcColorBlendFactor;
    public VkBlendFactor DstColorBlendFactor;
    public VkBlendOp ColorBlendOp;
    public VkBlendFactor SrcAlphaBlendFactor;
    public VkBlendFactor DstAlphaBlendFactor;
    public VkBlendOp AlphaBlendOp;
    public VkColorComponentFlags ColorWriteMask;
}

/// <summary>
/// 管线颜色混合状态创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineColorBlendStateCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint LogicOpEnable;
    public VkLogicOp LogicOp;
    public uint AttachmentCount;
    public VkPipelineColorBlendAttachmentState* PAttachments;
    public float BlendConstants0;
    public float BlendConstants1;
    public float BlendConstants2;
    public float BlendConstants3;
}

/// <summary>
/// 图形管线创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkGraphicsPipelineCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint StageCount;
    public VkPipelineShaderStageCreateInfo* PStages;
    public VkPipelineVertexInputStateCreateInfo* PVertexInputState;
    public VkPipelineInputAssemblyStateCreateInfo* PInputAssemblyState;
    public void* PTessellationState;
    public VkPipelineViewportStateCreateInfo* PViewportState;
    public VkPipelineRasterizationStateCreateInfo* PRasterizationState;
    public VkPipelineMultisampleStateCreateInfo* PMultisampleState;
    public VkPipelineDepthStencilStateCreateInfo* PDepthStencilState;
    public VkPipelineColorBlendStateCreateInfo* PColorBlendState;
    public void* PDynamicState;
    public VkPipelineLayout Layout;
    public VkRenderPass RenderPass;
    public uint Subpass;
    public VkPipeline BasePipelineHandle;
    public int BasePipelineIndex;
}

/// <summary>
/// 管线布局创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineLayoutCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint SetLayoutCount;
    public VkDescriptorSetLayout* PSetLayouts;
    public uint PushConstantRangeCount;
    public void* PPushConstantRanges;
}

#endregion

#region 渲染通道

/// <summary>
/// 附件描述
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkAttachmentDescription
{
    public uint Flags;
    public VkFormat Format;
    public VkSampleCountFlagBits Samples;
    public VkAttachmentLoadOp LoadOp;
    public VkAttachmentStoreOp StoreOp;
    public VkAttachmentLoadOp StencilLoadOp;
    public VkAttachmentStoreOp StencilStoreOp;
    public VkImageLayout InitialLayout;
    public VkImageLayout FinalLayout;
}

/// <summary>
/// 附件引用
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkAttachmentReference
{
    public uint Attachment;
    public VkImageLayout Layout;
}

/// <summary>
/// 子通道描述
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkSubpassDescription
{
    public uint Flags;
    public VkPipelineBindPoint PipelineBindPoint;
    public uint InputAttachmentCount;
    public VkAttachmentReference* PInputAttachments;
    public uint ColorAttachmentCount;
    public VkAttachmentReference* PColorAttachments;
    public VkAttachmentReference* PResolveAttachments;
    public VkAttachmentReference* PDepthStencilAttachment;
    public uint PreserveAttachmentCount;
    public uint* PPreserveAttachments;
}

/// <summary>
/// 子通道依赖
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkSubpassDependency
{
    public uint SrcSubpass;
    public uint DstSubpass;
    public VkPipelineStageFlagBits SrcStageMask;
    public VkPipelineStageFlagBits DstStageMask;
    public VkAccessFlagBits SrcAccessMask;
    public VkAccessFlagBits DstAccessMask;
    public VkDependencyFlags DependencyFlags;
}

/// <summary>
/// 渲染通道创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkRenderPassCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint AttachmentCount;
    public VkAttachmentDescription* PAttachments;
    public uint SubpassCount;
    public VkSubpassDescription* PSubpasses;
    public uint DependencyCount;
    public VkSubpassDependency* PDependencies;
}

/// <summary>
/// 帧缓冲创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkFramebufferCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public VkRenderPass RenderPass;
    public uint AttachmentCount;
    public VkImageView* PAttachments;
    public uint Width;
    public uint Height;
    public uint Layers;
}

/// <summary>
/// 渲染通道开始信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkRenderPassBeginInfo
{
    public VkStructureType SType;
    public void* PNext;
    public VkRenderPass RenderPass;
    public VkFramebuffer Framebuffer;
    public VkRect2D RenderArea;
    public uint ClearValueCount;
    public VkClearValue* PClearValues;
}

#endregion

#region 交换链

/// <summary>
/// 交换链创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkSwapchainCreateInfoKHR
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public VkSurfaceKHR Surface;
    public uint MinImageCount;
    public VkFormat ImageFormat;
    public VkColorSpaceKHR ImageColorSpace;
    public VkExtent2D ImageExtent;
    public uint ImageArrayLayers;
    public VkImageUsageFlagBits ImageUsage;
    public VkSharingMode ImageSharingMode;
    public uint QueueFamilyIndexCount;
    public uint* PQueueFamilyIndices;
    public VkSurfaceTransformFlagBitsKHR PreTransform;
    public VkCompositeAlphaFlagBitsKHR CompositeAlpha;
    public VkPresentModeKHR PresentMode;
    public uint Clipped;
    public VkSwapchainKHR OldSwapchain;
}

/// <summary>
/// 呈现信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPresentInfoKHR
{
    public VkStructureType SType;
    public void* PNext;
    public uint WaitSemaphoreCount;
    public VkSemaphore* PWaitSemaphores;
    public uint SwapchainCount;
    public VkSwapchainKHR* PSwapchains;
    public uint* PImageIndices;
    public void* PResults;
}

#endregion

#region 命令缓冲

/// <summary>
/// 命令池创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkCommandPoolCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public VkCommandPoolCreateFlagBits Flags;
    public uint QueueFamilyIndex;
}

/// <summary>
/// 命令缓冲区分配信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkCommandBufferAllocateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public VkCommandPool CommandPool;
    public VkCommandBufferLevel Level;
    public uint CommandBufferCount;
}

/// <summary>
/// 命令缓冲区开始信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkCommandBufferBeginInfo
{
    public VkStructureType SType;
    public void* PNext;
    public VkCommandBufferUsageFlagBits Flags;
    public void* PInheritanceInfo;
}

#endregion

#region 描述符

/// <summary>
/// 描述符集布局绑定
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDescriptorSetLayoutBinding
{
    public uint Binding;
    public VkDescriptorType DescriptorType;
    public uint DescriptorCount;
    public VkShaderStageFlagBits StageFlags;
    public VkSampler* PImmutableSamplers;
}

/// <summary>
/// 描述符集布局创建信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDescriptorSetLayoutCreateInfo
{
    public VkStructureType SType;
    public void* PNext;
    public uint Flags;
    public uint BindingCount;
    public VkDescriptorSetLayoutBinding* PBindings;
}

/// <summary>
/// 描述符缓冲区信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkDescriptorBufferInfo
{
    public VkBuffer Buffer;
    public ulong Offset;
    public ulong Range;
}

/// <summary>
/// 描述符图像信息
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VkDescriptorImageInfo
{
    public VkSampler Sampler;
    public VkImageView ImageView;
    public VkImageLayout ImageLayout;
}

/// <summary>
/// 写入描述符集
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkWriteDescriptorSet
{
    public VkStructureType SType;
    public void* PNext;
    public VkDescriptorSet DstSet;
    public uint DstBinding;
    public uint DstArrayElement;
    public uint DescriptorCount;
    public VkDescriptorType DescriptorType;
    public VkDescriptorImageInfo* PImageInfo;
    public VkDescriptorBufferInfo* PBufferInfo;
    public void* PTexelBufferView;
}

#endregion

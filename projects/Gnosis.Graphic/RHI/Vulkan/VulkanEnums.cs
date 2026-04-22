namespace Gnosis.Rendering.Backends.Vulkan;

#region 核心枚举

/// <summary>
/// Vulkan 操作结果
/// </summary>
public enum VkResult
{
    Success = 0,
    NotReady = 1,
    Timeout = 2,
    EventSet = 3,
    EventReset = 4,
    Incomplete = 5,
    ErrorOutOfHostMemory = -1,
    ErrorOutOfDeviceMemory = -2,
    ErrorInitializationFailed = -3,
    ErrorDeviceLost = -4,
    ErrorMemoryMapFailed = -5,
    ErrorLayerNotPresent = -6,
    ErrorExtensionNotPresent = -7,
    ErrorFeatureNotPresent = -8,
    ErrorIncompatibleDriver = -9,
    ErrorTooManyObjects = -10,
    ErrorFormatNotSupported = -11,
    ErrorFragmentedPool = -12,
    ErrorUnknown = -13,
    ErrorOutOfPoolMemory = -1000069000,
    ErrorInvalidExternalHandle = -1000072003,
    SuboptimalKHR = 1000001003
}

/// <summary>
/// Vulkan 数据格式
/// </summary>
public enum VkFormat
{
    Undefined = 0,
    R8G8B8A8Unorm = 37,
    B8G8R8A8Unorm = 44,
    R16G16B16A16Float = 91,
    R32G32B32A32Float = 109,
    D24UnormS8Uint = 129,
    D32FloatS8Uint = 130,
    D32Float = 126
}

/// <summary>
/// Vulkan 图像布局
/// </summary>
public enum VkImageLayout
{
    Undefined = 0,
    General = 1,
    ColorAttachmentOptimal = 2,
    DepthStencilAttachmentOptimal = 3,
    DepthStencilReadOnlyOptimal = 4,
    ShaderReadOnlyOptimal = 5,
    TransferSrcOptimal = 6,
    TransferDstOptimal = 7,
    Preinitialized = 8,
    DepthReadOnlyStencilAttachmentOptimal = 1000117000,
    DepthAttachmentStencilReadOnlyOptimal = 1000117001,
    PresentSrcKHR = 1000001002
}

/// <summary>
/// Vulkan 附件加载操作
/// </summary>
public enum VkAttachmentLoadOp
{
    Load = 0,
    Clear = 1,
    DontCare = 2
}

/// <summary>
/// Vulkan 附件存储操作
/// </summary>
public enum VkAttachmentStoreOp
{
    Store = 0,
    DontCare = 1
}

/// <summary>
/// Vulkan 图元拓扑
/// </summary>
public enum VkPrimitiveTopology
{
    PointList = 0,
    LineList = 1,
    LineStrip = 2,
    TriangleList = 3,
    TriangleStrip = 4,
    TriangleFan = 5
}

/// <summary>
/// Vulkan 裁剪模式标志
/// </summary>
[Flags]
public enum VkCullModeFlags
{
    None = 0,
    Front = 1,
    Back = 2,
    FrontAndBack = 3
}

/// <summary>
/// Vulkan 正面方向
/// </summary>
public enum VkFrontFace
{
    CounterClockwise = 0,
    Clockwise = 1
}

/// <summary>
/// Vulkan 多边形填充模式
/// </summary>
public enum VkPolygonMode
{
    Fill = 0,
    Line = 1,
    Point = 2
}

/// <summary>
/// Vulkan 混合因子
/// </summary>
public enum VkBlendFactor
{
    Zero = 0,
    One = 1,
    SrcColor = 2,
    OneMinusSrcColor = 3,
    DstColor = 4,
    OneMinusDstColor = 5,
    SrcAlpha = 6,
    OneMinusSrcAlpha = 7,
    DstAlpha = 8,
    OneMinusDstAlpha = 9,
    ConstantColor = 10,
    OneMinusConstantColor = 11,
    ConstantAlpha = 12,
    OneMinusConstantAlpha = 13,
    SrcAlphaSaturate = 14
}

/// <summary>
/// Vulkan 混合操作
/// </summary>
public enum VkBlendOp
{
    Add = 0,
    Subtract = 1,
    ReverseSubtract = 2,
    Min = 3,
    Max = 4
}

/// <summary>
/// Vulkan 比较操作
/// </summary>
public enum VkCompareOp
{
    Never = 0,
    Less = 1,
    Equal = 2,
    LessOrEqual = 3,
    Greater = 4,
    NotEqual = 5,
    GreaterOrEqual = 6,
    Always = 7
}

/// <summary>
/// Vulkan 模板操作
/// </summary>
public enum VkStencilOp
{
    Keep = 0,
    Zero = 1,
    Replace = 2,
    IncrementAndClamp = 3,
    DecrementAndClamp = 4,
    Invert = 5,
    IncrementAndWrap = 6,
    DecrementAndWrap = 7
}

/// <summary>
/// Vulkan 采样计数标志位
/// </summary>
[Flags]
public enum VkSampleCountFlagBits
{
    None = 0,
    _1 = 1,
    _2 = 2,
    _4 = 4,
    _8 = 8,
    _16 = 16,
    _32 = 32,
    _64 = 64
}

/// <summary>
/// Vulkan 呈现模式
/// </summary>
public enum VkPresentModeKHR
{
    Immediate = 0,
    Mailbox = 1,
    Fifo = 2,
    FifoRelaxed = 3
}

/// <summary>
/// Vulkan 颜色空间
/// </summary>
public enum VkColorSpaceKHR
{
    SrgbNonlinear = 0
}

/// <summary>
/// Vulkan 结构体类型
/// </summary>
public enum VkStructureType
{
    ApplicationInfo = 0,
    InstanceCreateInfo = 1,
    DeviceQueueCreateInfo = 2,
    DeviceCreateInfo = 3,
    SubmitInfo = 4,
    MemoryAllocateInfo = 5,
    BindBufferMemoryInfo = 6,
    BindImageMemoryInfo = 7,
    RenderPassCreateInfo = 8,
    ImageViewCreateInfo = 9,
    ShaderModuleCreateInfo = 10,
    PipelineCacheCreateInfo = 11,
    PipelineShaderStageCreateInfo = 12,
    PipelineVertexInputStateCreateInfo = 13,
    PipelineInputAssemblyStateCreateInfo = 14,
    PipelineTessellationStateCreateInfo = 15,
    PipelineViewportStateCreateInfo = 16,
    PipelineRasterizationStateCreateInfo = 17,
    PipelineMultisampleStateCreateInfo = 18,
    PipelineDepthStencilStateCreateInfo = 19,
    PipelineColorBlendStateCreateInfo = 20,
    PipelineDynamicStateCreateInfo = 21,
    GraphicsPipelineCreateInfo = 22,
    SwapchainCreateInfoKHR = 1000001000,
    PresentInfoKHR = 1000001001,
    RenderPassBeginInfo = 43,
    CommandBufferBeginInfo = 44,
    FramebufferCreateInfo = 36,
    CommandPoolCreateInfo = 39,
    SemaphoreCreateInfo = 55,
    FenceCreateInfo = 56
}

/// <summary>
/// Vulkan 管线绑定点
/// </summary>
public enum VkPipelineBindPoint
{
    Graphics = 0,
    Compute = 1
}

/// <summary>
/// Vulkan 着色器阶段标志位
/// </summary>
[Flags]
public enum VkShaderStageFlagBits
{
    Vertex = 1,
    TessellationControl = 2,
    TessellationEvaluation = 4,
    Geometry = 8,
    Fragment = 16,
    Compute = 32,
    AllGraphics = 0x7FFFFFFF,
    All = 0x7FFFFFFF
}

/// <summary>
/// Vulkan 图像用途标志位
/// </summary>
[Flags]
public enum VkImageUsageFlagBits
{
    TransferSrc = 1,
    TransferDst = 2,
    Sampled = 4,
    Storage = 8,
    ColorAttachment = 16,
    DepthStencilAttachment = 32,
    TransientAttachment = 64,
    InputAttachment = 128
}

/// <summary>
/// Vulkan 缓冲区用途标志位
/// </summary>
[Flags]
public enum VkBufferUsageFlagBits
{
    TransferSrc = 1,
    TransferDst = 2,
    UniformTexelBuffer = 4,
    StorageTexelBuffer = 8,
    UniformBuffer = 16,
    StorageBuffer = 32,
    IndexBuffer = 64,
    VertexBuffer = 128,
    IndirectBuffer = 256
}

/// <summary>
/// Vulkan 内存属性标志位
/// </summary>
[Flags]
public enum VkMemoryPropertyFlagBits
{
    DeviceLocal = 1,
    HostVisible = 2,
    HostCoherent = 4,
    HostCached = 8,
    LazilyAllocated = 16
}

/// <summary>
/// Vulkan 命令缓冲区用途标志位
/// </summary>
[Flags]
public enum VkCommandBufferUsageFlagBits
{
    OneTimeSubmit = 1,
    RenderPassContinue = 2,
    SimultaneousUse = 4
}

/// <summary>
/// Vulkan 访问标志位
/// </summary>
[Flags]
public enum VkAccessFlagBits
{
    IndirectCommandRead = 1,
    IndexRead = 2,
    VertexAttributeRead = 4,
    UniformRead = 8,
    InputAttachmentRead = 16,
    ShaderRead = 32,
    ShaderWrite = 64,
    ColorAttachmentRead = 128,
    ColorAttachmentWrite = 256,
    DepthStencilAttachmentRead = 512,
    DepthStencilAttachmentWrite = 1024,
    TransferRead = 2048,
    TransferWrite = 4096,
    HostRead = 8192,
    HostWrite = 16384,
    MemoryRead = 32768,
    MemoryWrite = 65536
}

/// <summary>
/// Vulkan 管线阶段标志位
/// </summary>
[Flags]
public enum VkPipelineStageFlagBits
{
    TopOfPipe = 1,
    DrawIndirect = 2,
    VertexInput = 4,
    VertexShader = 8,
    TessellationControlShader = 16,
    TessellationEvaluationShader = 32,
    GeometryShader = 64,
    FragmentShader = 128,
    EarlyFragmentTests = 256,
    LateFragmentTests = 512,
    ColorAttachmentOutput = 1024,
    ComputeShader = 2048,
    Transfer = 4096,
    BottomOfPipe = 8192,
    Host = 16384,
    AllGraphics = 0x7FFFF,
    AllCommands = 0x7FFFFF
}

/// <summary>
/// Vulkan 图像视图类型
/// </summary>
public enum VkImageViewType
{
    _1D = 0,
    _2D = 1,
    _3D = 2,
    Cube = 3,
    _1DArray = 4,
    _2DArray = 5,
    CubeArray = 6
}

/// <summary>
/// Vulkan 分量重映射
/// </summary>
public enum VkComponentSwizzle
{
    Identity = 0,
    Zero = 1,
    One = 2,
    R = 3,
    G = 4,
    B = 5,
    A = 6
}

/// <summary>
/// Vulkan 描述符类型
/// </summary>
public enum VkDescriptorType
{
    Sampler = 0,
    CombinedImageSampler = 1,
    SampledImage = 2,
    StorageImage = 3,
    UniformTexelBuffer = 4,
    StorageTexelBuffer = 5,
    UniformBuffer = 6,
    StorageBuffer = 7,
    UniformBufferDynamic = 8,
    StorageBufferDynamic = 9,
    InputAttachment = 10
}

/// <summary>
/// Vulkan 过滤器
/// </summary>
public enum VkFilter
{
    Nearest = 0,
    Linear = 1
}

/// <summary>
/// Vulkan 采样器寻址模式
/// </summary>
public enum VkSamplerAddressMode
{
    Repeat = 0,
    MirroredRepeat = 1,
    ClampToEdge = 2,
    ClampToBorder = 3,
    MirrorClampToEdge = 4
}

/// <summary>
/// Vulkan 命令池创建标志位
/// </summary>
[Flags]
public enum VkCommandPoolCreateFlagBits
{
    Transient = 1,
    ResetCommandBuffer = 2
}

#endregion

#region 附加枚举

/// <summary>
/// Vulkan 共享模式
/// </summary>
public enum VkSharingMode
{
    Exclusive = 0,
    Concurrent = 1
}

/// <summary>
/// Vulkan 图像类型
/// </summary>
public enum VkImageType
{
    _1D = 0,
    _2D = 1,
    _3D = 2
}

/// <summary>
/// Vulkan 图像平铺模式
/// </summary>
public enum VkImageTiling
{
    Optimal = 0,
    Linear = 1
}

/// <summary>
/// Vulkan 图像方面标志位
/// </summary>
[Flags]
public enum VkImageAspectFlagBits
{
    Color = 1,
    Depth = 2,
    Stencil = 4,
    Metadata = 8
}

/// <summary>
/// Vulkan 颜色分量标志位
/// </summary>
[Flags]
public enum VkColorComponentFlags
{
    R = 1,
    G = 2,
    B = 4,
    A = 8
}

/// <summary>
/// Vulkan 逻辑操作
/// </summary>
public enum VkLogicOp
{
    Clear = 0,
    And = 1,
    AndReverse = 2,
    Copy = 3,
    AndInverted = 4,
    NoOp = 5,
    Xor = 6,
    Or = 7,
    Nor = 8,
    Equiv = 9,
    Invert = 10,
    OrReverse = 11,
    CopyInverted = 12,
    OrInverted = 13,
    Nand = 14,
    Set = 15
}

/// <summary>
/// Vulkan 依赖标志位
/// </summary>
[Flags]
public enum VkDependencyFlags
{
    ByRegion = 1,
    DeviceGroup = 2,
    ViewLocal = 4
}

/// <summary>
/// Vulkan 表面变换标志位
/// </summary>
[Flags]
public enum VkSurfaceTransformFlagBitsKHR
{
    Identity = 1,
    Rotate90 = 2,
    Rotate180 = 4,
    Rotate270 = 8,
    HorizontalMirror = 16,
    HorizontalMirrorRotate90 = 32,
    HorizontalMirrorRotate180 = 64,
    HorizontalMirrorRotate270 = 128,
    Inherit = 256
}

/// <summary>
/// Vulkan 合成 Alpha 标志位
/// </summary>
[Flags]
public enum VkCompositeAlphaFlagBitsKHR
{
    Opaque = 1,
    PreMultiplied = 2,
    PostMultiplied = 4,
    Inherit = 8
}

/// <summary>
/// Vulkan 命令缓冲区级别
/// </summary>
public enum VkCommandBufferLevel
{
    Primary = 0,
    Secondary = 1
}

/// <summary>
/// Vulkan 子通道内容
/// </summary>
public enum VkSubpassContents
{
    Inline = 0,
    SecondaryCommandBuffers = 1
}

/// <summary>
/// Vulkan 索引类型
/// </summary>
public enum VkIndexType
{
    Uint16 = 0,
    Uint32 = 1
}

#endregion

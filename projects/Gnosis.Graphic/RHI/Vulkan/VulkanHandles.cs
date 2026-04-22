using System.Runtime.InteropServices;

namespace Gnosis.Graphic.RHI.Vulkan;

/// <summary>
/// Vulkan 不透明句柄定义
/// </summary>
public readonly struct VkInstance : IEquatable<VkInstance>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkInstance Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 实例句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkInstance(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkInstance h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkInstance other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkInstance h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkInstance left, VkInstance right) => left.Equals(right);

    public static bool operator !=(VkInstance left, VkInstance right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 物理设备句柄
/// </summary>
public readonly struct VkPhysicalDevice : IEquatable<VkPhysicalDevice>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkPhysicalDevice Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 物理设备句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkPhysicalDevice(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkPhysicalDevice h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkPhysicalDevice other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkPhysicalDevice h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkPhysicalDevice left, VkPhysicalDevice right) => left.Equals(right);

    public static bool operator !=(VkPhysicalDevice left, VkPhysicalDevice right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 逻辑设备句柄
/// </summary>
public readonly struct VkDevice : IEquatable<VkDevice>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkDevice Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 逻辑设备句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkDevice(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkDevice h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkDevice other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkDevice h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkDevice left, VkDevice right) => left.Equals(right);

    public static bool operator !=(VkDevice left, VkDevice right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 队列句柄
/// </summary>
public readonly struct VkQueue : IEquatable<VkQueue>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkQueue Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 队列句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkQueue(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkQueue h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkQueue other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkQueue h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkQueue left, VkQueue right) => left.Equals(right);

    public static bool operator !=(VkQueue left, VkQueue right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 命令池句柄
/// </summary>
public readonly struct VkCommandPool : IEquatable<VkCommandPool>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkCommandPool Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 命令池句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkCommandPool(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkCommandPool h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkCommandPool other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkCommandPool h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkCommandPool left, VkCommandPool right) => left.Equals(right);

    public static bool operator !=(VkCommandPool left, VkCommandPool right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 命令缓冲区句柄
/// </summary>
public readonly struct VkCommandBuffer : IEquatable<VkCommandBuffer>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkCommandBuffer Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 命令缓冲区句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkCommandBuffer(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkCommandBuffer h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkCommandBuffer other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkCommandBuffer h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkCommandBuffer left, VkCommandBuffer right) => left.Equals(right);

    public static bool operator !=(VkCommandBuffer left, VkCommandBuffer right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 渲染通道句柄
/// </summary>
public readonly struct VkRenderPass : IEquatable<VkRenderPass>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkRenderPass Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 渲染通道句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkRenderPass(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkRenderPass h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkRenderPass other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkRenderPass h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkRenderPass left, VkRenderPass right) => left.Equals(right);

    public static bool operator !=(VkRenderPass left, VkRenderPass right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 帧缓冲句柄
/// </summary>
public readonly struct VkFramebuffer : IEquatable<VkFramebuffer>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkFramebuffer Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 帧缓冲句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkFramebuffer(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkFramebuffer h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkFramebuffer other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkFramebuffer h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkFramebuffer left, VkFramebuffer right) => left.Equals(right);

    public static bool operator !=(VkFramebuffer left, VkFramebuffer right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 交换链句柄
/// </summary>
public readonly struct VkSwapchainKHR : IEquatable<VkSwapchainKHR>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkSwapchainKHR Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 交换链句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkSwapchainKHR(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkSwapchainKHR h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkSwapchainKHR other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkSwapchainKHR h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkSwapchainKHR left, VkSwapchainKHR right) => left.Equals(right);

    public static bool operator !=(VkSwapchainKHR left, VkSwapchainKHR right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 着色器模块句柄
/// </summary>
public readonly struct VkShaderModule : IEquatable<VkShaderModule>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkShaderModule Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 着色器模块句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkShaderModule(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkShaderModule h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkShaderModule other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkShaderModule h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkShaderModule left, VkShaderModule right) => left.Equals(right);

    public static bool operator !=(VkShaderModule left, VkShaderModule right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 管线布局句柄
/// </summary>
public readonly struct VkPipelineLayout : IEquatable<VkPipelineLayout>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkPipelineLayout Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 管线布局句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkPipelineLayout(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkPipelineLayout h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkPipelineLayout other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkPipelineLayout h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkPipelineLayout left, VkPipelineLayout right) => left.Equals(right);

    public static bool operator !=(VkPipelineLayout left, VkPipelineLayout right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 管线句柄
/// </summary>
public readonly struct VkPipeline : IEquatable<VkPipeline>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkPipeline Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 管线句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkPipeline(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkPipeline h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkPipeline other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkPipeline h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkPipeline left, VkPipeline right) => left.Equals(right);

    public static bool operator !=(VkPipeline left, VkPipeline right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 管线缓存句柄
/// </summary>
public readonly struct VkPipelineCache : IEquatable<VkPipelineCache>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkPipelineCache Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 管线缓存句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkPipelineCache(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkPipelineCache h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkPipelineCache other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkPipelineCache h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkPipelineCache left, VkPipelineCache right) => left.Equals(right);

    public static bool operator !=(VkPipelineCache left, VkPipelineCache right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 缓冲区句柄
/// </summary>
public readonly struct VkBuffer : IEquatable<VkBuffer>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkBuffer Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 缓冲区句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkBuffer(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkBuffer h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkBuffer other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkBuffer h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkBuffer left, VkBuffer right) => left.Equals(right);

    public static bool operator !=(VkBuffer left, VkBuffer right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 图像句柄
/// </summary>
public readonly struct VkImage : IEquatable<VkImage>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkImage Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 图像句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkImage(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkImage h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkImage other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkImage h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkImage left, VkImage right) => left.Equals(right);

    public static bool operator !=(VkImage left, VkImage right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 图像视图句柄
/// </summary>
public readonly struct VkImageView : IEquatable<VkImageView>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkImageView Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 图像视图句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkImageView(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkImageView h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkImageView other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkImageView h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkImageView left, VkImageView right) => left.Equals(right);

    public static bool operator !=(VkImageView left, VkImageView right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 设备内存句柄
/// </summary>
public readonly struct VkDeviceMemory : IEquatable<VkDeviceMemory>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkDeviceMemory Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 设备内存句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkDeviceMemory(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkDeviceMemory h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkDeviceMemory other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkDeviceMemory h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkDeviceMemory left, VkDeviceMemory right) => left.Equals(right);

    public static bool operator !=(VkDeviceMemory left, VkDeviceMemory right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 栅栏句柄
/// </summary>
public readonly struct VkFence : IEquatable<VkFence>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkFence Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 栅栏句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkFence(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkFence h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkFence other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkFence h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkFence left, VkFence right) => left.Equals(right);

    public static bool operator !=(VkFence left, VkFence right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 信号量句柄
/// </summary>
public readonly struct VkSemaphore : IEquatable<VkSemaphore>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkSemaphore Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 信号量句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkSemaphore(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkSemaphore h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkSemaphore other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkSemaphore h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkSemaphore left, VkSemaphore right) => left.Equals(right);

    public static bool operator !=(VkSemaphore left, VkSemaphore right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 描述符集句柄
/// </summary>
public readonly struct VkDescriptorSet : IEquatable<VkDescriptorSet>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkDescriptorSet Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 描述符集句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkDescriptorSet(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkDescriptorSet h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkDescriptorSet other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkDescriptorSet h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkDescriptorSet left, VkDescriptorSet right) => left.Equals(right);

    public static bool operator !=(VkDescriptorSet left, VkDescriptorSet right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 描述符集布局句柄
/// </summary>
public readonly struct VkDescriptorSetLayout : IEquatable<VkDescriptorSetLayout>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkDescriptorSetLayout Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 描述符集布局句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkDescriptorSetLayout(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkDescriptorSetLayout h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkDescriptorSetLayout other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkDescriptorSetLayout h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkDescriptorSetLayout left, VkDescriptorSetLayout right) => left.Equals(right);

    public static bool operator !=(VkDescriptorSetLayout left, VkDescriptorSetLayout right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 表面句柄
/// </summary>
public readonly struct VkSurfaceKHR : IEquatable<VkSurfaceKHR>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkSurfaceKHR Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 表面句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkSurfaceKHR(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkSurfaceKHR h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkSurfaceKHR other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkSurfaceKHR h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkSurfaceKHR left, VkSurfaceKHR right) => left.Equals(right);

    public static bool operator !=(VkSurfaceKHR left, VkSurfaceKHR right) => !left.Equals(right);
}

/// <summary>
/// Vulkan 采样器句柄
/// </summary>
public readonly struct VkSampler : IEquatable<VkSampler>
{
    private readonly nint _handle;

    /// <summary>
    /// 空句柄
    /// </summary>
    public static VkSampler Null => new(nint.Zero);

    /// <summary>
    /// 构造 Vulkan 采样器句柄
    /// </summary>
    /// <param name="handle">原生句柄值</param>
    public VkSampler(nint handle) => _handle = handle;

    /// <summary>
    /// 是否为空句柄
    /// </summary>
    public bool IsNull => _handle == nint.Zero;

    /// <summary>
    /// 显式转换为原生句柄
    /// </summary>
    public static explicit operator nint(VkSampler h) => h._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public bool Equals(VkSampler other) => _handle == other._handle;

    /// <summary>
    /// 判断是否相等
    /// </summary>
    public override bool Equals(object? obj) => obj is VkSampler h && Equals(h);

    /// <summary>
    /// 获取哈希值
    /// </summary>
    public override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(VkSampler left, VkSampler right) => left.Equals(right);

    public static bool operator !=(VkSampler left, VkSampler right) => !left.Equals(right);
}

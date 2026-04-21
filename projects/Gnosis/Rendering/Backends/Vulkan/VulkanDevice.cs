namespace Gnosis.Rendering.Backends.Vulkan;

/// <summary>
/// Vulkan 图形设备，基于 Khronos Vulkan API 实现
/// </summary>
public sealed class VulkanDevice : RHI.IDevice
{
    #region 属性

    /// <summary>
    /// Vulkan 实例句柄
    /// </summary>
    public nint Instance { get; private set; }

    /// <summary>
    /// Vulkan 物理设备句柄
    /// </summary>
    public nint PhysicalDevice { get; private set; }

    /// <summary>
    /// Vulkan 逻辑设备句柄
    /// </summary>
    public nint Device { get; private set; }

    /// <summary>
    /// 是否启用验证层
    /// </summary>
    public bool EnableValidationLayers { get; set; } = true;

    /// <summary>
    /// 是否启用光线追踪扩展
    /// </summary>
    public bool EnableRayTracing { get; set; } = false;

    #endregion

    #region IDevice 实现

    /// <summary>
    /// 创建 Vulkan 缓冲区
    /// </summary>
    /// <param name="size">缓冲区大小（字节）</param>
    /// <returns>缓冲区资源</returns>
    public RHI.IResource CreateBuffer(ulong size)
    {
        throw new NotImplementedException("Vulkan 后端尚未实现");
    }

    /// <summary>
    /// 创建 Vulkan 纹理
    /// </summary>
    /// <param name="width">纹理宽度</param>
    /// <param name="height">纹理高度</param>
    /// <param name="format">纹理格式</param>
    /// <returns>纹理资源</returns>
    public RHI.IResource CreateTexture(uint width, uint height, RHI.ResourceFormat format)
    {
        throw new NotImplementedException("Vulkan 后端尚未实现");
    }

    /// <summary>
    /// 创建 Vulkan 着色器模块
    /// </summary>
    /// <param name="spirvBytecode">SPIR-V 字节码</param>
    /// <returns>着色器资源</returns>
    public RHI.IResource CreateShader(byte[] spirvBytecode)
    {
        throw new NotImplementedException("Vulkan 后端尚未实现");
    }

    /// <summary>
    /// 创建 Vulkan 管线状态
    /// </summary>
    /// <param name="desc">管线状态描述</param>
    /// <returns>管线状态对象</returns>
    public RHI.IPipelineState CreatePipelineState(RHI.PipelineStateDesc desc)
    {
        throw new NotImplementedException("Vulkan 后端尚未实现");
    }

    /// <summary>
    /// 创建 Vulkan 命令缓冲区
    /// </summary>
    /// <returns>命令表对象</returns>
    public RHI.ICommandTable CreateCommandTable()
    {
        throw new NotImplementedException("Vulkan 后端尚未实现");
    }

    /// <summary>
    /// 提交 Vulkan 命令缓冲区到 GPU 队列
    /// </summary>
    /// <param name="commandTable">待执行的命令表</param>
    public void Submit(RHI.ICommandTable commandTable)
    {
        throw new NotImplementedException("Vulkan 后端尚未实现");
    }

    /// <summary>
    /// 等待 Vulkan 设备空闲
    /// </summary>
    public void WaitIdle()
    {
        throw new NotImplementedException("Vulkan 后端尚未实现");
    }

    #endregion
}

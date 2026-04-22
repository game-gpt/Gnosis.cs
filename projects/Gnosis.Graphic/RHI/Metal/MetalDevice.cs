using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.RHI.Metal;

/// <summary>
/// Metal 图形设备，基于 Apple Metal API 实现
/// </summary>
public sealed class MetalDevice : RHI.IDevice
{
    #region 属性

    /// <summary>
    /// MTLDevice 句柄
    /// </summary>
    public nint DeviceHandle { get; private set; }

    /// <summary>
    /// MTLCommandQueue 句柄
    /// </summary>
    public nint CommandQueueHandle { get; private set; }

    /// <summary>
    /// 是否启用 Metal 性能着色器
    /// </summary>
    public bool EnablePerformanceShaders { get; set; } = true;

    /// <summary>
    /// 是否启用 Metal 光线追踪
    /// </summary>
    public bool EnableRayTracing { get; set; } = false;

    #endregion

    #region IDevice 实现

    /// <summary>
    /// 创建 Metal 缓冲区
    /// </summary>
    public RHI.IResource CreateBuffer(in RHI.BufferDesc desc)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 纹理
    /// </summary>
    public RHI.IResource CreateTexture(in RHI.TextureDesc desc)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 采样器
    /// </summary>
    public RHI.IResource CreateSampler(in RHI.SamplerDesc desc)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 着色器库
    /// </summary>
    public RHI.IResource CreateShader(in RHI.ShaderDesc desc)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 渲染管线状态
    /// </summary>
    public RHI.IPipelineState CreatePipelineState(in RHI.PipelineStateDesc desc)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 渲染通道
    /// </summary>
    public RHI.IRhiRenderPass CreateRenderPass(in RHI.RenderPassDesc desc)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 帧缓冲
    /// </summary>
    public RHI.IRhiFramebuffer CreateFramebuffer(in RHI.FramebufferDesc desc)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 交换链
    /// </summary>
    public RHI.IRhiSwapchain CreateSwapchain(in RHI.SwapchainDesc desc)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 围栏
    /// </summary>
    public RHI.IRhiFence CreateFence(bool signaled = false)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 信号量
    /// </summary>
    public RHI.IRhiSemaphore CreateSemaphore()
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 命令缓冲区
    /// </summary>
    public RHI.ICommandTable CreateCommandTable()
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 描述符集
    /// </summary>
    public RHI.IRhiDescriptorSet CreateDescriptorSet(RHI.DescriptorSetBinding[] bindings)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 提交 Metal 命令缓冲区
    /// </summary>
    public void Submit(RHI.ICommandTable commandTable, RHI.IRhiFence? signalFence = null)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 等待 Metal 设备空闲
    /// </summary>
    public void WaitIdle()
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 释放 Metal 设备资源
    /// </summary>
    public void Dispose()
    {
    }

    #endregion
}

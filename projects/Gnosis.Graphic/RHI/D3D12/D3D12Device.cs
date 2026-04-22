namespace Gnosis.Rendering.Backends.D3D12;

/// <summary>
/// Direct3D 12 图形设备，基于 Microsoft D3D12 API 实现
/// </summary>
public sealed class D3D12Device : RHI.IDevice
{
    #region 属性

    /// <summary>
    /// IDXGIAdapter 句柄
    /// </summary>
    public nint AdapterHandle { get; private set; }

    /// <summary>
    /// ID3D12Device 句柄
    /// </summary>
    public nint DeviceHandle { get; private set; }

    /// <summary>
    /// ID3D12CommandQueue 句柄
    /// </summary>
    public nint CommandQueueHandle { get; private set; }

    /// <summary>
    /// 是否启用 DirectX 光线追踪（DXR）
    /// </summary>
    public bool EnableRayTracing { get; set; } = false;

    /// <summary>
    /// 是否启用可变速率着色（VRS）
    /// </summary>
    public bool EnableVariableRateShading { get; set; } = false;

    /// <summary>
    /// 是否启用网格着色器
    /// </summary>
    public bool EnableMeshShaders { get; set; } = false;

    #endregion

    #region IDevice 实现

    /// <summary>
    /// 创建 D3D12 缓冲区
    /// </summary>
    public RHI.IResource CreateBuffer(in RHI.BufferDesc desc)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 纹理
    /// </summary>
    public RHI.IResource CreateTexture(in RHI.TextureDesc desc)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 采样器
    /// </summary>
    public RHI.IResource CreateSampler(in RHI.SamplerDesc desc)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 着色器
    /// </summary>
    public RHI.IResource CreateShader(in RHI.ShaderDesc desc)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 管线状态
    /// </summary>
    public RHI.IPipelineState CreatePipelineState(in RHI.PipelineStateDesc desc)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 渲染通道
    /// </summary>
    public RHI.IRhiRenderPass CreateRenderPass(in RHI.RenderPassDesc desc)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 帧缓冲
    /// </summary>
    public RHI.IRhiFramebuffer CreateFramebuffer(in RHI.FramebufferDesc desc)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 交换链
    /// </summary>
    public RHI.IRhiSwapchain CreateSwapchain(in RHI.SwapchainDesc desc)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 围栏
    /// </summary>
    public RHI.IRhiFence CreateFence(bool signaled = false)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 信号量
    /// </summary>
    public RHI.IRhiSemaphore CreateSemaphore()
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 命令列表
    /// </summary>
    public RHI.ICommandTable CreateCommandTable()
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 描述符集
    /// </summary>
    public RHI.IRhiDescriptorSet CreateDescriptorSet(RHI.DescriptorSetBinding[] bindings)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 提交 D3D12 命令列表到命令队列
    /// </summary>
    public void Submit(RHI.ICommandTable commandTable, RHI.IRhiFence? signalFence = null)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 等待 D3D12 命令队列空闲
    /// </summary>
    public void WaitIdle()
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 释放 D3D12 设备资源
    /// </summary>
    public void Dispose()
    {
    }

    #endregion
}

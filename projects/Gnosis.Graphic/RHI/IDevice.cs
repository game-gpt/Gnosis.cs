namespace Gnosis.Graphic.RHI;

/// <summary>
/// 图形设备接口，提供资源创建和命令提交能力
/// </summary>
public interface IDevice : IDisposable
{
    /// <summary>
    /// 创建缓冲区资源
    /// </summary>
    /// <param name="desc">缓冲区描述</param>
    /// <returns>缓冲区资源</returns>
    IResource CreateBuffer(in BufferDesc desc);

    /// <summary>
    /// 创建纹理资源
    /// </summary>
    /// <param name="desc">纹理描述</param>
    /// <returns>纹理资源</returns>
    IResource CreateTexture(in TextureDesc desc);

    /// <summary>
    /// 创建采样器
    /// </summary>
    /// <param name="desc">采样器描述</param>
    /// <returns>采样器资源</returns>
    IResource CreateSampler(in SamplerDesc desc);

    /// <summary>
    /// 创建着色器模块
    /// </summary>
    /// <param name="desc">着色器描述</param>
    /// <returns>着色器资源</returns>
    IResource CreateShader(in ShaderDesc desc);

    /// <summary>
    /// 创建管线状态对象
    /// </summary>
    /// <param name="desc">管线状态描述</param>
    /// <returns>管线状态对象</returns>
    IPipelineState CreatePipelineState(in PipelineStateDesc desc);

    /// <summary>
    /// 创建渲染通道
    /// </summary>
    /// <param name="desc">渲染通道描述</param>
    /// <returns>渲染通道对象</returns>
    IRhiRenderPass CreateRenderPass(in RenderPassDesc desc);

    /// <summary>
    /// 创建帧缓冲
    /// </summary>
    /// <param name="desc">帧缓冲描述</param>
    /// <returns>帧缓冲对象</returns>
    IRhiFramebuffer CreateFramebuffer(in FramebufferDesc desc);

    /// <summary>
    /// 创建交换链
    /// </summary>
    /// <param name="desc">交换链描述</param>
    /// <returns>交换链对象</returns>
    IRhiSwapchain CreateSwapchain(in SwapchainDesc desc);

    /// <summary>
    /// 创建围栏
    /// </summary>
    /// <param name="signaled">是否创建为已触发状态</param>
    /// <returns>围栏对象</returns>
    IRhiFence CreateFence(bool signaled = false);

    /// <summary>
    /// 创建信号量
    /// </summary>
    /// <returns>信号量对象</returns>
    IRhiSemaphore CreateSemaphore();

    /// <summary>
    /// 创建描述符集
    /// </summary>
    /// <param name="bindings">描述符绑定列表</param>
    /// <returns>描述符集对象</returns>
    IRhiDescriptorSet CreateDescriptorSet(DescriptorSetBinding[] bindings);

    /// <summary>
    /// 创建命令表
    /// </summary>
    /// <returns>命令表对象</returns>
    ICommandTable CreateCommandTable();

    /// <summary>
    /// 提交命令表到 GPU 执行
    /// </summary>
    /// <param name="commandTable">待执行的命令表</param>
    /// <param name="signalFence">执行完成后触发的围栏</param>
    void Submit(ICommandTable commandTable, IRhiFence? signalFence = null);

    /// <summary>
    /// 等待设备空闲
    /// </summary>
    void WaitIdle();
}

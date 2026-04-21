namespace Gnosis.Rendering.Backends.Metal;

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
    /// <param name="size">缓冲区大小（字节）</param>
    /// <returns>缓冲区资源</returns>
    public RHI.IResource CreateBuffer(ulong size)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 纹理
    /// </summary>
    /// <param name="width">纹理宽度</param>
    /// <param name="height">纹理高度</param>
    /// <param name="format">纹理格式</param>
    /// <returns>纹理资源</returns>
    public RHI.IResource CreateTexture(uint width, uint height, RHI.ResourceFormat format)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 着色器库
    /// </summary>
    /// <param name="spirvBytecode">SPIR-V 字节码（需交叉编译为 MSL）</param>
    /// <returns>着色器资源</returns>
    public RHI.IResource CreateShader(byte[] spirvBytecode)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 渲染管线状态
    /// </summary>
    /// <param name="desc">管线状态描述</param>
    /// <returns>管线状态对象</returns>
    public RHI.IPipelineState CreatePipelineState(RHI.PipelineStateDesc desc)
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 创建 Metal 命令缓冲区
    /// </summary>
    /// <returns>命令表对象</returns>
    public RHI.ICommandTable CreateCommandTable()
    {
        throw new NotImplementedException("Metal 后端尚未实现");
    }

    /// <summary>
    /// 提交 Metal 命令缓冲区
    /// </summary>
    /// <param name="commandTable">待执行的命令表</param>
    public void Submit(RHI.ICommandTable commandTable)
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

    #endregion
}

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
    /// 创建 D3D12 默认堆缓冲区
    /// </summary>
    /// <param name="size">缓冲区大小（字节）</param>
    /// <returns>缓冲区资源</returns>
    public RHI.IResource CreateBuffer(ulong size)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 提交纹理
    /// </summary>
    /// <param name="width">纹理宽度</param>
    /// <param name="height">纹理高度</param>
    /// <param name="format">纹理格式</param>
    /// <returns>纹理资源</returns>
    public RHI.IResource CreateTexture(uint width, uint height, RHI.ResourceFormat format)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 根签名和管线状态
    /// </summary>
    /// <param name="spirvBytecode">SPIR-V 字节码（需交叉编译为 DXIL）</param>
    /// <returns>着色器资源</returns>
    public RHI.IResource CreateShader(byte[] spirvBytecode)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 PSO
    /// </summary>
    /// <param name="desc">管线状态描述</param>
    /// <returns>管线状态对象</returns>
    public RHI.IPipelineState CreatePipelineState(RHI.PipelineStateDesc desc)
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 创建 D3D12 命令分配器和命令列表
    /// </summary>
    /// <returns>命令表对象</returns>
    public RHI.ICommandTable CreateCommandTable()
    {
        throw new NotImplementedException("D3D12 后端尚未实现");
    }

    /// <summary>
    /// 提交 D3D12 命令列表到命令队列
    /// </summary>
    /// <param name="commandTable">待执行的命令表</param>
    public void Submit(RHI.ICommandTable commandTable)
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

    #endregion
}

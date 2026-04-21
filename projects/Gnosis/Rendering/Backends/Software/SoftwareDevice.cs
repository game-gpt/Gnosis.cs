using Gnosis.Rendering.RHI;

namespace Gnosis.Rendering.Backends.Software;

/// <summary>
/// 软件渲染设备，实现 IDevice 接口的 CPU 端渲染
/// </summary>
public sealed class SoftwareDevice : IDevice
{
    #region 字段

    private ulong _nextResourceId;

    #endregion

    #region IDevice 实现

    /// <summary>
    /// 创建缓冲区资源
    /// </summary>
    /// <param name="size">缓冲区大小（字节）</param>
    /// <returns>缓冲区资源</returns>
    public IResource CreateBuffer(ulong size)
    {
        return new SoftwareResource(++_nextResourceId, ResourceType.Buffer);
    }

    /// <summary>
    /// 创建纹理资源
    /// </summary>
    /// <param name="width">纹理宽度</param>
    /// <param name="height">纹理高度</param>
    /// <param name="format">纹理格式</param>
    /// <returns>纹理资源</returns>
    public IResource CreateTexture(uint width, uint height, ResourceFormat format)
    {
        return new SoftwareResource(++_nextResourceId, ResourceType.Texture2D);
    }

    /// <summary>
    /// 创建着色器资源
    /// </summary>
    /// <param name="spirvBytecode">SPIR-V 字节码</param>
    /// <returns>着色器资源</returns>
    public IResource CreateShader(byte[] spirvBytecode)
    {
        return new SoftwareResource(++_nextResourceId, ResourceType.Shader);
    }

    /// <summary>
    /// 创建管线状态
    /// </summary>
    /// <param name="desc">管线状态描述</param>
    /// <returns>管线状态对象</returns>
    public IPipelineState CreatePipelineState(PipelineStateDesc desc)
    {
        return new SoftwarePipelineState(desc);
    }

    /// <summary>
    /// 创建命令表
    /// </summary>
    /// <returns>命令表对象</returns>
    public ICommandTable CreateCommandTable()
    {
        return new SoftwareCommandTable();
    }

    /// <summary>
    /// 提交命令表执行
    /// </summary>
    /// <param name="commandTable">待执行的命令表</param>
    public void Submit(ICommandTable commandTable)
    {
    }

    /// <summary>
    /// 等待设备空闲
    /// </summary>
    public void WaitIdle()
    {
    }

    #endregion

    #region 内部类型

    private sealed class SoftwareResource : IResource
    {
        private bool _isDisposed;

        public ulong Id { get; }
        public ResourceType ResourceType { get; }
        public bool IsDisposed => _isDisposed;

        public SoftwareResource(ulong id, ResourceType resourceType)
        {
            Id = id;
            ResourceType = resourceType;
        }

        public void Dispose()
        {
            _isDisposed = true;
        }
    }

    private sealed class SoftwarePipelineState : IPipelineState
    {
        public BlendMode BlendMode { get; }
        public bool DepthTest { get; }
        public CullMode CullMode { get; }
        public ulong ShaderHandle { get; }

        public SoftwarePipelineState(PipelineStateDesc desc)
        {
            BlendMode = desc.BlendMode;
            DepthTest = desc.DepthTest;
            CullMode = desc.CullMode;
            ShaderHandle = desc.ShaderHandle;
        }
    }

    private sealed class SoftwareCommandTable : ICommandTable
    {
        public void Begin()
        {
        }

        public void End()
        {
        }

        public void SetPipelineState(IPipelineState pipelineState)
        {
        }

        public void SetVertexBuffer(IResource buffer, ulong offset = 0)
        {
        }

        public void SetIndexBuffer(IResource buffer, ulong offset = 0)
        {
        }

        public void SetTexture(IResource texture, uint slot)
        {
        }

        public void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
        {
        }

        public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        {
        }

        public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
        }
    }

    #endregion
}

using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.RHI.Software;

/// <summary>
/// 软件渲染设备，实现 IDevice 接口的 CPU 端渲染
/// </summary>
public sealed class SoftwareDevice : IDevice
{
    #region 字段

    private ulong _nextResourceId;
    private bool _isDisposed;

    #endregion

    #region IDevice 实现

    /// <summary>
    /// 创建缓冲区资源
    /// </summary>
    /// <param name="desc">缓冲区描述</param>
    /// <returns>缓冲区资源</returns>
    public IResource CreateBuffer(in BufferDesc desc)
    {
        return new SoftwareResource(++_nextResourceId, ResourceType.Buffer, ResourceFormat.Unknown, desc.Size);
    }

    /// <summary>
    /// 创建纹理资源
    /// </summary>
    /// <param name="desc">纹理描述</param>
    /// <returns>纹理资源</returns>
    public IResource CreateTexture(in TextureDesc desc)
    {
        var resourceType = desc.Dimension switch
        {
            TextureDimension.Dim1D => ResourceType.Texture1D,
            TextureDimension.Dim2D => ResourceType.Texture2D,
            TextureDimension.Dim3D => ResourceType.Texture3D,
            _ => ResourceType.Texture2D
        };

        ulong size = (ulong)desc.Width * desc.Height * desc.Depth * 4;
        return new SoftwareResource(++_nextResourceId, resourceType, desc.Format, size);
    }

    /// <summary>
    /// 创建采样器
    /// </summary>
    /// <param name="desc">采样器描述</param>
    /// <returns>采样器资源</returns>
    public IResource CreateSampler(in SamplerDesc desc)
    {
        return new SoftwareResource(++_nextResourceId, ResourceType.Sampler, ResourceFormat.Unknown, 0);
    }

    /// <summary>
    /// 创建着色器模块
    /// </summary>
    /// <param name="desc">着色器描述</param>
    /// <returns>着色器资源</returns>
    public IResource CreateShader(in ShaderDesc desc)
    {
        ulong size = (ulong)desc.Bytecode.Length;
        return new SoftwareResource(++_nextResourceId, ResourceType.Shader, ResourceFormat.Unknown, size);
    }

    /// <summary>
    /// 创建管线状态对象
    /// </summary>
    /// <param name="desc">管线状态描述</param>
    /// <returns>管线状态对象</returns>
    public IPipelineState CreatePipelineState(in PipelineStateDesc desc)
    {
        return new SoftwarePipelineState(desc);
    }

    /// <summary>
    /// 创建渲染通道
    /// </summary>
    /// <param name="desc">渲染通道描述</param>
    /// <returns>渲染通道对象</returns>
    public IRhiRenderPass CreateRenderPass(in RenderPassDesc desc)
    {
        return new SoftwareRenderPass(desc);
    }

    /// <summary>
    /// 创建帧缓冲
    /// </summary>
    /// <param name="desc">帧缓冲描述</param>
    /// <returns>帧缓冲对象</returns>
    public IRhiFramebuffer CreateFramebuffer(in FramebufferDesc desc)
    {
        return new SoftwareFramebuffer(desc);
    }

    /// <summary>
    /// 创建交换链
    /// </summary>
    /// <param name="desc">交换链描述</param>
    /// <returns>交换链对象</returns>
    public IRhiSwapchain CreateSwapchain(in SwapchainDesc desc)
    {
        return new SoftwareSwapchain(desc);
    }

    /// <summary>
    /// 创建围栏
    /// </summary>
    /// <param name="signaled">是否创建为已触发状态</param>
    /// <returns>围栏对象</returns>
    public IRhiFence CreateFence(bool signaled = false)
    {
        return new SoftwareFence(signaled);
    }

    /// <summary>
    /// 创建信号量
    /// </summary>
    /// <returns>信号量对象</returns>
    public IRhiSemaphore CreateSemaphore()
    {
        return new SoftwareSemaphore();
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
    /// 创建描述符集
    /// </summary>
    /// <param name="bindings">描述符绑定列表</param>
    /// <returns>描述符集对象</returns>
    public IRhiDescriptorSet CreateDescriptorSet(DescriptorSetBinding[] bindings)
    {
        return new SoftwareDescriptorSet();
    }

    /// <summary>
    /// 提交命令表到 GPU 执行
    /// </summary>
    /// <param name="commandTable">待执行的命令表</param>
    /// <param name="signalFence">执行完成后触发的围栏</param>
    public void Submit(ICommandTable commandTable, IRhiFence? signalFence = null)
    {
        if (signalFence is SoftwareFence fence)
        {
            fence.Signal();
        }
    }

    /// <summary>
    /// 等待设备空闲
    /// </summary>
    public void WaitIdle()
    {
    }

    /// <summary>
    /// 释放设备资源
    /// </summary>
    public void Dispose()
    {
        _isDisposed = true;
    }

    #endregion

    #region 内部类型 - SoftwareResource

    /// <summary>
    /// 软件渲染资源，实现 IResource 接口
    /// </summary>
    private sealed class SoftwareResource : IResource
    {
        #region 字段

        private bool _isDisposed;

        #endregion

        #region 属性

        /// <summary>
        /// 资源唯一标识
        /// </summary>
        public ulong Id { get; }

        /// <summary>
        /// 资源类型
        /// </summary>
        public ResourceType ResourceType { get; }

        /// <summary>
        /// 资源格式
        /// </summary>
        public ResourceFormat Format { get; }

        /// <summary>
        /// 资源大小（字节）
        /// </summary>
        public ulong Size { get; }

        /// <summary>
        /// 是否已释放
        /// </summary>
        public bool IsDisposed => _isDisposed;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化软件渲染资源
        /// </summary>
        /// <param name="id">资源唯一标识</param>
        /// <param name="resourceType">资源类型</param>
        /// <param name="format">资源格式</param>
        /// <param name="size">资源大小</param>
        public SoftwareResource(ulong id, ResourceType resourceType, ResourceFormat format, ulong size)
        {
            Id = id;
            ResourceType = resourceType;
            Format = format;
            Size = size;
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
        }

        #endregion
    }

    #endregion

    #region 内部类型 - SoftwarePipelineState

    /// <summary>
    /// 软件管线状态，实现 IPipelineState 接口
    /// </summary>
    private sealed class SoftwarePipelineState : IPipelineState
    {
        #region 字段

        private bool _isDisposed;

        #endregion

        #region 属性

        /// <summary>
        /// 混合模式
        /// </summary>
        public BlendMode BlendMode { get; }

        /// <summary>
        /// 深度测试启用
        /// </summary>
        public bool DepthTest { get; }

        /// <summary>
        /// 深度写入启用
        /// </summary>
        public bool DepthWrite { get; }

        /// <summary>
        /// 深度比较函数
        /// </summary>
        public CompareFunction DepthCompare { get; }

        /// <summary>
        /// 剔除模式
        /// </summary>
        public CullMode CullMode { get; }

        /// <summary>
        /// 正面朝向
        /// </summary>
        public FrontFace FrontFace { get; }

        /// <summary>
        /// 多边形模式
        /// </summary>
        public PolygonMode PolygonMode { get; }

        /// <summary>
        /// 拓扑类型
        /// </summary>
        public PrimitiveTopology Topology { get; }

        /// <summary>
        /// 着色器句柄
        /// </summary>
        public ulong ShaderHandle { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化软件管线状态
        /// </summary>
        /// <param name="desc">管线状态描述</param>
        public SoftwarePipelineState(PipelineStateDesc desc)
        {
            BlendMode = desc.BlendMode;
            DepthTest = desc.DepthTest;
            DepthWrite = desc.DepthWrite;
            DepthCompare = desc.DepthCompare;
            CullMode = desc.CullMode;
            FrontFace = desc.FrontFace;
            PolygonMode = desc.PolygonMode;
            Topology = desc.Topology;
            ShaderHandle = desc.ShaderHandle;
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放管线状态
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
        }

        #endregion
    }

    #endregion

    #region 内部类型 - SoftwareCommandTable

    /// <summary>
    /// 软件命令表，实现 ICommandTable 接口
    /// </summary>
    private sealed class SoftwareCommandTable : ICommandTable
    {
        #region 字段

        private bool _isDisposed;

        #endregion

        #region ICommandTable 实现

        /// <summary>
        /// 开始录制命令
        /// </summary>
        public void Begin()
        {
        }

        /// <summary>
        /// 结束录制命令
        /// </summary>
        public void End()
        {
        }

        /// <summary>
        /// 开始渲染通道
        /// </summary>
        /// <param name="renderPass">渲染通道</param>
        /// <param name="framebuffer">帧缓冲</param>
        /// <param name="clearColors">清除颜色列表</param>
        /// <param name="clearDepth">清除深度值</param>
        /// <param name="clearStencil">清除模板值</param>
        public void BeginRenderPass(IRhiRenderPass renderPass, IRhiFramebuffer framebuffer, IReadOnlyList<(float r, float g, float b, float a)>? clearColors = null, float clearDepth = 1.0f, byte clearStencil = 0)
        {
        }

        /// <summary>
        /// 结束渲染通道
        /// </summary>
        public void EndRenderPass()
        {
        }

        /// <summary>
        /// 绑定管线状态
        /// </summary>
        /// <param name="pipelineState">管线状态对象</param>
        public void SetPipelineState(IPipelineState pipelineState)
        {
        }

        /// <summary>
        /// 设置视口
        /// </summary>
        public void SetViewport(float x, float y, float width, float height, float minDepth = 0.0f, float maxDepth = 1.0f)
        {
        }

        /// <summary>
        /// 设置裁剪矩形
        /// </summary>
        public void SetScissor(int x, int y, uint width, uint height)
        {
        }

        /// <summary>
        /// 绑定顶点缓冲区
        /// </summary>
        public void SetVertexBuffer(IResource buffer, ulong offset = 0)
        {
        }

        /// <summary>
        /// 绑定索引缓冲区
        /// </summary>
        public void SetIndexBuffer(IResource buffer, ulong offset = 0)
        {
        }

        /// <summary>
        /// 清除渲染目标
        /// </summary>
        /// <param name="attachmentIndex">附件索引</param>
        /// <param name="r">红色分量</param>
        /// <param name="g">绿色分量</param>
        /// <param name="b">蓝色分量</param>
        /// <param name="a">透明度分量</param>
        public void ClearRenderTarget(uint attachmentIndex, float r, float g, float b, float a)
        {
        }

        /// <summary>
        /// 清除深度模板
        /// </summary>
        /// <param name="depth">深度值</param>
        /// <param name="stencil">模板值</param>
        public void ClearDepthStencil(float depth, byte stencil)
        {
        }

        /// <summary>
        /// 绑定描述符集
        /// </summary>
        /// <param name="descriptorSet">描述符集</param>
        /// <param name="setIndex">描述符集索引</param>
        public void BindDescriptorSet(IRhiDescriptorSet descriptorSet, uint setIndex)
        {
        }

        /// <summary>
        /// 非索引绘制
        /// </summary>
        public void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
        {
        }

        /// <summary>
        /// 索引绘制
        /// </summary>
        public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        {
        }

        /// <summary>
        /// 计算调度
        /// </summary>
        public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
        }

        /// <summary>
        /// 管线屏障，确保内存访问顺序
        /// </summary>
        public void PipelineBarrier(PipelineStageFlag srcStage, PipelineStageFlag dstStage, AccessFlag srcAccess, AccessFlag dstAccess)
        {
        }

        /// <summary>
        /// 复制资源
        /// </summary>
        public void CopyResource(IResource src, IResource dst)
        {
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放命令表
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
        }

        #endregion
    }

    #endregion

    #region 内部类型 - SoftwareRenderPass

    /// <summary>
    /// 软件渲染通道，实现 IRhiRenderPass 接口
    /// </summary>
    private sealed class SoftwareRenderPass : IRhiRenderPass
    {
        #region 字段

        private bool _isDisposed;

        #endregion

        #region 属性

        /// <summary>
        /// 原生句柄
        /// </summary>
        public nint Handle => nint.Zero;

        /// <summary>
        /// 附件数量
        /// </summary>
        public uint AttachmentCount { get; }

        /// <summary>
        /// 子通道数量
        /// </summary>
        public uint SubPassCount { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化软件渲染通道
        /// </summary>
        /// <param name="desc">渲染通道描述</param>
        public SoftwareRenderPass(in RenderPassDesc desc)
        {
            AttachmentCount = (uint)desc.Attachments.Length;
            SubPassCount = (uint)desc.SubPasses.Length;
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放渲染通道
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
        }

        #endregion
    }

    #endregion

    #region 内部类型 - SoftwareFramebuffer

    /// <summary>
    /// 软件帧缓冲，实现 IRhiFramebuffer 接口
    /// </summary>
    private sealed class SoftwareFramebuffer : IRhiFramebuffer
    {
        #region 字段

        private bool _isDisposed;
        private readonly List<IResource> _attachments;

        #endregion

        #region 属性

        /// <summary>
        /// 原生句柄
        /// </summary>
        public nint Handle => nint.Zero;

        /// <summary>
        /// 帧缓冲宽度
        /// </summary>
        public uint Width { get; }

        /// <summary>
        /// 帧缓冲高度
        /// </summary>
        public uint Height { get; }

        /// <summary>
        /// 附件资源列表
        /// </summary>
        public IReadOnlyList<IResource> Attachments => _attachments;

        /// <summary>
        /// 关联的渲染通道
        /// </summary>
        public IRhiRenderPass RenderPass { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化软件帧缓冲
        /// </summary>
        /// <param name="desc">帧缓冲描述</param>
        public SoftwareFramebuffer(in FramebufferDesc desc)
        {
            Width = desc.Width;
            Height = desc.Height;
            RenderPass = desc.RenderPass;
            _attachments = new List<IResource>(desc.Attachments);
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放帧缓冲
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
        }

        #endregion
    }

    #endregion

    #region 内部类型 - SoftwareSwapchain

    /// <summary>
    /// 软件交换链，实现 IRhiSwapchain 接口
    /// </summary>
    private sealed class SoftwareSwapchain : IRhiSwapchain
    {
        #region 字段

        private bool _isDisposed;
        private uint _currentImageIndex;

        #endregion

        #region 属性

        /// <summary>
        /// 交换链宽度
        /// </summary>
        public uint Width { get; private set; }

        /// <summary>
        /// 交换链高度
        /// </summary>
        public uint Height { get; private set; }

        /// <summary>
        /// 交换链图像格式
        /// </summary>
        public ResourceFormat Format { get; }

        /// <summary>
        /// 交换链图像数量
        /// </summary>
        public uint ImageCount { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化软件交换链
        /// </summary>
        /// <param name="desc">交换链描述</param>
        public SoftwareSwapchain(in SwapchainDesc desc)
        {
            Width = desc.Width;
            Height = desc.Height;
            Format = desc.Format;
            ImageCount = desc.ImageCount;
            _currentImageIndex = 0;
        }

        #endregion

        #region IRhiSwapchain 实现

        /// <summary>
        /// 获取下一帧可呈现图像的索引
        /// </summary>
        /// <param name="semaphore">信号量，图像可用时触发</param>
        /// <param name="fence">围栏，图像可用时触发</param>
        /// <returns>下一帧图像索引</returns>
        public uint AcquireNextImage(IRhiSemaphore? semaphore, IRhiFence? fence)
        {
            uint index = _currentImageIndex;
            _currentImageIndex = (_currentImageIndex + 1) % ImageCount;
            return index;
        }

        /// <summary>
        /// 呈现当前帧图像
        /// </summary>
        /// <param name="waitSemaphores">等待的信号量列表</param>
        public void Present(IReadOnlyList<IRhiSemaphore> waitSemaphores)
        {
        }

        /// <summary>
        /// 调整交换链尺寸
        /// </summary>
        /// <param name="width">新宽度</param>
        /// <param name="height">新高度</param>
        public void Resize(uint width, uint height)
        {
            Width = width;
            Height = height;
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放交换链
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
        }

        #endregion
    }

    #endregion

    #region 内部类型 - SoftwareFence

    /// <summary>
    /// 软件围栏，实现 IRhiFence 接口
    /// </summary>
    private sealed class SoftwareFence : IRhiFence
    {
        #region 字段

        private volatile bool _signaled;
        private bool _isDisposed;

        #endregion

        #region 属性

        /// <summary>
        /// 围栏是否已触发
        /// </summary>
        public bool IsSignaled => _signaled;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化软件围栏
        /// </summary>
        /// <param name="signaled">是否创建为已触发状态</param>
        public SoftwareFence(bool signaled)
        {
            _signaled = signaled;
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 触发围栏
        /// </summary>
        public void Signal()
        {
            _signaled = true;
        }

        #endregion

        #region IRhiFence 实现

        /// <summary>
        /// 等待围栏触发
        /// </summary>
        /// <param name="timeout">超时时间（纳秒），ulong.MaxValue 表示无限等待</param>
        public void Wait(ulong timeout = ulong.MaxValue)
        {
        }

        /// <summary>
        /// 重置围栏为未触发状态
        /// </summary>
        public void Reset()
        {
            _signaled = false;
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放围栏
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
        }

        #endregion
    }

    #endregion

    #region 内部类型 - SoftwareSemaphore

    /// <summary>
    /// 软件信号量，实现 IRhiSemaphore 接口
    /// </summary>
    private sealed class SoftwareSemaphore : IRhiSemaphore
    {
        #region 字段

        private bool _isDisposed;

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放信号量
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
        }

        #endregion
    }

    #endregion

    #region 内部类型 - SoftwareDescriptorSet

    /// <summary>
    /// 软件描述符集，实现 IRhiDescriptorSet 接口
    /// </summary>
    private unsafe class SoftwareDescriptorSet : IRhiDescriptorSet
    {
        #region 字段

        private bool _isDisposed;

        #endregion

        #region IRhiDescriptorSet 实现

        /// <summary>
        /// 绑定缓冲区到描述符集
        /// </summary>
        /// <param name="binding">绑定槽位</param>
        /// <param name="buffer">缓冲区资源</param>
        /// <param name="offset">偏移量</param>
        /// <param name="range">范围</param>
        public void BindBuffer(uint binding, IResource buffer, ulong offset = 0, ulong range = ulong.MaxValue)
        {
        }

        /// <summary>
        /// 绑定纹理到描述符集
        /// </summary>
        /// <param name="binding">绑定槽位</param>
        /// <param name="texture">纹理资源</param>
        public void BindTexture(uint binding, IResource texture)
        {
        }

        /// <summary>
        /// 绑定采样器到描述符集
        /// </summary>
        /// <param name="binding">绑定槽位</param>
        /// <param name="sampler">采样器资源</param>
        public void BindSampler(uint binding, IResource sampler)
        {
        }

        /// <summary>
        /// 绑定 Uniform 数据到描述符集
        /// </summary>
        /// <param name="binding">绑定槽位</param>
        /// <param name="data">数据指针</param>
        /// <param name="size">数据大小</param>
        public void BindUniformData(uint binding, void* data, ulong size)
        {
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放描述符集
        /// </summary>
        public void Dispose()
        {
            _isDisposed = true;
        }

        #endregion
    }

    #endregion
}

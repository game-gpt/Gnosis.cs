using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.FX;

/// <summary>
/// GPU 粒子渲染器，负责将 GPU 模拟的粒子绘制到屏幕
/// </summary>
public sealed class GpuParticleRenderer : IParticleRenderer, IDisposable
{
    #region 字段

    private readonly IDevice _device;
    private IRhiRenderPass? _renderPass;
    private IRhiFramebuffer? _framebuffer;
    private IPipelineState? _pipelineState;
    private IRhiDescriptorSet? _descriptorSet;
    private IResource? _vertexBuffer;
    private IResource? _sampler;
    private bool _isDisposed;

    #endregion

    #region 属性

    public ParticleRenderMode RenderMode { get; set; }
    public string? MaterialPath { get; set; }
    public float CameraVelocityScale { get; set; }
    public float VelocityScale { get; set; }
    public float LengthScale { get; set; }
    public bool RenderAlignment { get; set; }

    /// <summary>
    /// 关联的着色器程序，需在外部设置后调用 RebuildPipeline
    /// </summary>
    public IShaderProgram? ShaderProgram { get; set; }

    #endregion

    #region 构造函数

    public GpuParticleRenderer(IDevice device)
    {
        _device = device;

        RenderMode = ParticleRenderMode.Billboard;
        MaterialPath = null;
        CameraVelocityScale = 0.0f;
        VelocityScale = 0.0f;
        LengthScale = 1.0f;
        RenderAlignment = false;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 初始化渲染器资源
    /// </summary>
    public void Initialize()
    {
        CreateQuadVertexBuffer();
        CreateSampler();
        CreateDescriptorSet();
    }

    public void Render()
    {
    }

    /// <summary>
    /// 渲染粒子
    /// </summary>
    /// <param name="commandTable">命令表</param>
    /// <param name="particleBuffer">粒子数据缓冲区</param>
    /// <param name="aliveBuffer">活跃粒子索引缓冲区</param>
    /// <param name="aliveCount">活跃粒子数量</param>
    /// <param name="width">渲染目标宽度</param>
    /// <param name="height">渲染目标高度</param>
    public void Render(ICommandTable commandTable, IResource particleBuffer, IResource aliveBuffer, int aliveCount, uint width, uint height)
    {
        if (aliveCount <= 0 || _pipelineState is null)
        {
            return;
        }

        EnsureRenderResources(width, height);

        if (_renderPass is null || _framebuffer is null)
        {
            return;
        }

        UpdateDescriptorSet(particleBuffer, aliveBuffer);

        commandTable.BeginRenderPass(_renderPass, _framebuffer);
        commandTable.SetPipelineState(_pipelineState);
        commandTable.SetViewport(0, 0, width, height);
        commandTable.SetScissor(0, 0, width, height);

        if (_descriptorSet is not null)
        {
            commandTable.BindDescriptorSet(_descriptorSet, 0);
        }

        if (_vertexBuffer is not null)
        {
            commandTable.SetVertexBuffer(_vertexBuffer);
        }

        commandTable.Draw(4, (uint)aliveCount);

        commandTable.EndRenderPass();
    }

    /// <summary>
    /// 重建管线状态，在设置 ShaderProgram 后调用
    /// </summary>
    public void RebuildPipeline()
    {
        if (_device is null || ShaderProgram is null)
        {
            return;
        }

        _pipelineState?.Dispose();

        _pipelineState = _device.CreatePipelineState(new PipelineStateDesc
        {
            Shader = ShaderProgram,
            Topology = PrimitiveTopology.TriangleStrip,
            BlendMode = BlendMode.Additive,
            DepthTest = true,
            DepthWrite = false,
            CullMode = CullMode.None,
            ColorAttachmentCount = 1,
            ColorFormats = [ResourceFormat.R16G16B16A16Float]
        });
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _renderPass?.Dispose();
        _framebuffer?.Dispose();
        _pipelineState?.Dispose();
        _descriptorSet?.Dispose();
        _vertexBuffer?.Dispose();
        _sampler?.Dispose();

        _renderPass = null;
        _framebuffer = null;
        _pipelineState = null;
        _descriptorSet = null;
        _vertexBuffer = null;
        _sampler = null;

        _isDisposed = true;
    }

    #endregion

    #region 私有方法

    private void CreateQuadVertexBuffer()
    {
        float[] vertices =
        [
            -0.5f, -0.5f, 0.0f, 0.0f,
             0.5f, -0.5f, 1.0f, 0.0f,
             0.5f,  0.5f, 1.0f, 1.0f,
            -0.5f,  0.5f, 0.0f, 1.0f
        ];

        _vertexBuffer = _device.CreateBuffer(new BufferDesc
        {
            Size = (ulong)(vertices.Length * sizeof(float)),
            Usage = BufferUsage.VertexBuffer | BufferUsage.TransferDst,
            DeviceLocal = true
        });
    }

    private void CreateSampler()
    {
        _sampler = _device.CreateSampler(new SamplerDesc
        {
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge
        });
    }

    private void CreateDescriptorSet()
    {
        _descriptorSet = _device.CreateDescriptorSet(
        [
            new DescriptorSetBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.StorageBuffer,
                StageFlags = ShaderStageFlag.Vertex
            },
            new DescriptorSetBinding
            {
                Binding = 1,
                DescriptorType = DescriptorType.StorageBuffer,
                StageFlags = ShaderStageFlag.Vertex
            },
            new DescriptorSetBinding
            {
                Binding = 2,
                DescriptorType = DescriptorType.CombinedImageSampler,
                StageFlags = ShaderStageFlag.Fragment
            }
        ]);
    }

    private void EnsureRenderResources(uint width, uint height)
    {
        if (_renderPass is not null)
        {
            return;
        }

        _renderPass = _device.CreateRenderPass(new RenderPassDesc
        {
            Attachments =
            [
                new AttachmentDesc
                {
                    Format = ResourceFormat.R16G16B16A16Float,
                    LoadAction = LoadAction.Load,
                    StoreAction = StoreAction.Store,
                    InitialLayout = TextureLayout.ShaderReadOnly,
                    FinalLayout = TextureLayout.ShaderReadOnly
                }
            ],
            SubPasses =
            [
                new SubPassDesc
                {
                    ColorAttachments = [0]
                }
            ]
        });
    }

    private void UpdateDescriptorSet(IResource particleBuffer, IResource aliveBuffer)
    {
        if (_descriptorSet is null)
        {
            return;
        }

        _descriptorSet.BindBuffer(0, particleBuffer);
        _descriptorSet.BindBuffer(1, aliveBuffer);
    }

    #endregion
}

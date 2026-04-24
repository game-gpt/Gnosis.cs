using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.PostProcess;

/// <summary>
/// 色调映射模式
/// </summary>
public enum ToneMappingMode
{
    Off,
    ACES,
    Reinhard,
    Uncharted2,
    AgX
}

/// <summary>
/// 色调映射后处理效果
/// </summary>
public sealed class ToneMappingEffect : PostProcessEffect
{
    #region 常量

    private const uint BindingInputTexture = 0;
    private const uint BindingParameters = 1;

    #endregion

    #region 字段

    private IRhiRenderPass? _renderPass;
    private IRhiFramebuffer? _framebuffer;
    private IPipelineState? _pipelineState;
    private IRhiDescriptorSet? _descriptorSet;
    private IResource? _vertexBuffer;
    private IResource? _indexBuffer;
    private IResource? _sampler;
    private IResource? _parameterBuffer;
    private IShaderProgram? _shaderProgram;
    private uint _currentWidth;
    private uint _currentHeight;

    #endregion

    #region 属性

    public ToneMappingMode Mode { get; set; }
    public float Exposure { get; set; }
    public float WhitePoint { get; set; }

    /// <summary>
    /// 关联的着色器程序，需在外部设置后调用 RebuildPipeline
    /// </summary>
    public IShaderProgram? ShaderProgram
    {
        get => _shaderProgram;
        set => _shaderProgram = value;
    }

    #endregion

    #region 构造函数

    public ToneMappingEffect() : base("ToneMapping", 0)
    {
        Mode = ToneMappingMode.ACES;
        Exposure = 1.0f;
        WhitePoint = 11.2f;
    }

    #endregion

    #region 公开方法

    public override void Initialize()
    {
        if (Device is null)
        {
            return;
        }

        CreateScreenQuadBuffers();
        CreateSampler();
        CreateParameterBuffer();

        base.Initialize();
    }

    public override void Dispose()
    {
        _renderPass?.Dispose();
        _framebuffer?.Dispose();
        _pipelineState?.Dispose();
        _descriptorSet?.Dispose();
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _sampler?.Dispose();
        _parameterBuffer?.Dispose();

        _renderPass = null;
        _framebuffer = null;
        _pipelineState = null;
        _descriptorSet = null;
        _vertexBuffer = null;
        _indexBuffer = null;
        _sampler = null;
        _parameterBuffer = null;

        base.Dispose();
    }

    public override void Setup(ICommandTable commandTable, uint width, uint height)
    {
        if (Device is null || !IsInitialized)
        {
            return;
        }

        if (_renderPass is null || _currentWidth != width || _currentHeight != height)
        {
            _currentWidth = width;
            _currentHeight = height;
            CreateRenderPassAndFramebuffer(width, height);
        }
    }

    public override void Execute(ICommandTable commandTable, IResource inputTexture, IResource outputTexture)
    {
        if (Device is null || !IsInitialized || _renderPass is null || _framebuffer is null)
        {
            return;
        }

        UpdateParameters();
        UpdateDescriptorSet(inputTexture);

        commandTable.BeginRenderPass(_renderPass, _framebuffer);

        if (_pipelineState is not null)
        {
            commandTable.SetPipelineState(_pipelineState);
        }

        commandTable.SetViewport(0, 0, _currentWidth, _currentHeight);
        commandTable.SetScissor(0, 0, _currentWidth, _currentHeight);

        if (_descriptorSet is not null)
        {
            commandTable.BindDescriptorSet(_descriptorSet, 0);
        }

        if (_vertexBuffer is not null)
        {
            commandTable.SetVertexBuffer(_vertexBuffer);
        }

        if (_indexBuffer is not null)
        {
            commandTable.SetIndexBuffer(_indexBuffer);
            commandTable.DrawIndexed(6);
        }
        else
        {
            commandTable.Draw(6);
        }

        commandTable.EndRenderPass();
    }

    /// <summary>
    /// 重建管线状态，在设置 ShaderProgram 后调用
    /// </summary>
    public void RebuildPipeline()
    {
        if (Device is null || _shaderProgram is null)
        {
            return;
        }

        _pipelineState?.Dispose();

        _pipelineState = Device.CreatePipelineState(new PipelineStateDesc
        {
            Shader = _shaderProgram,
            Topology = PrimitiveTopology.TriangleList,
            BlendMode = BlendMode.None,
            DepthTest = false,
            DepthWrite = false,
            CullMode = CullMode.None,
            ColorAttachmentCount = 1,
            ColorFormats = [ResourceFormat.R16G16B16A16Float]
        });
    }

    #endregion

    #region 私有方法

    private void CreateScreenQuadBuffers()
    {
        if (Device is null)
        {
            return;
        }

        float[] vertices =
        [
            -1.0f, -1.0f, 0.0f, 0.0f,
             1.0f, -1.0f, 1.0f, 0.0f,
             1.0f,  1.0f, 1.0f, 1.0f,
            -1.0f,  1.0f, 0.0f, 1.0f
        ];

        _vertexBuffer = Device.CreateBuffer(new BufferDesc
        {
            Size = (ulong)(vertices.Length * sizeof(float)),
            Usage = BufferUsage.VertexBuffer | BufferUsage.TransferDst,
            DeviceLocal = true
        });

        uint[] indices = [0, 1, 2, 0, 2, 3];

        _indexBuffer = Device.CreateBuffer(new BufferDesc
        {
            Size = (ulong)(indices.Length * sizeof(uint)),
            Usage = BufferUsage.IndexBuffer | BufferUsage.TransferDst,
            DeviceLocal = true
        });
    }

    private void CreateSampler()
    {
        if (Device is null)
        {
            return;
        }

        _sampler = Device.CreateSampler(new SamplerDesc
        {
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge
        });
    }

    private void CreateParameterBuffer()
    {
        if (Device is null)
        {
            return;
        }

        _parameterBuffer = Device.CreateBuffer(new BufferDesc
        {
            Size = 256,
            Usage = BufferUsage.UniformBuffer | BufferUsage.TransferDst,
            DeviceLocal = true
        });
    }

    private void CreateRenderPassAndFramebuffer(uint width, uint height)
    {
        if (Device is null)
        {
            return;
        }

        _renderPass?.Dispose();
        _framebuffer?.Dispose();
        _descriptorSet?.Dispose();

        _renderPass = Device.CreateRenderPass(new RenderPassDesc
        {
            Attachments =
            [
                new AttachmentDesc
                {
                    Format = ResourceFormat.R16G16B16A16Float,
                    LoadAction = LoadAction.Clear,
                    StoreAction = StoreAction.Store,
                    InitialLayout = TextureLayout.Undefined,
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

        _descriptorSet = Device.CreateDescriptorSet(
        [
            new DescriptorSetBinding
            {
                Binding = BindingInputTexture,
                DescriptorType = DescriptorType.CombinedImageSampler,
                StageFlags = ShaderStageFlag.Fragment
            },
            new DescriptorSetBinding
            {
                Binding = BindingParameters,
                DescriptorType = DescriptorType.UniformBuffer,
                StageFlags = ShaderStageFlag.Fragment
            }
        ]);
    }

    private unsafe void UpdateParameters()
    {
        if (_descriptorSet is null || _parameterBuffer is null)
        {
            return;
        }

        var parameters = new ToneMappingParameters
        {
            Mode = (int)Mode,
            Exposure = Exposure,
            WhitePoint = WhitePoint,
            Padding = 0.0f
        };

        _descriptorSet.BindBuffer(BindingParameters, _parameterBuffer);
    }

    private void UpdateDescriptorSet(IResource inputTexture)
    {
        if (_descriptorSet is null)
        {
            return;
        }

        _descriptorSet.BindTexture(BindingInputTexture, inputTexture);

        if (_sampler is not null)
        {
            _descriptorSet.BindSampler(BindingInputTexture, _sampler);
        }
    }

    #endregion

    #region 嵌套类型

    private struct ToneMappingParameters
    {
        public int Mode;
        public float Exposure;
        public float WhitePoint;
        public float Padding;
    }

    #endregion
}

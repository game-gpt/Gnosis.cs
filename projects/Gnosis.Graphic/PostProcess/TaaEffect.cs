using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.PostProcess;

/// <summary>
/// TAA 质量等级
/// </summary>
public enum TaaQuality
{
    Low = 0,
    Medium = 1,
    High = 2
}

/// <summary>
/// 时序抗锯齿后处理效果
/// 使用子像素抖动投影 + 历史帧重投影 + 邻域裁剪实现抗锯齿
/// </summary>
public sealed class TaaEffect : PostProcessEffect
{
    #region 常量

    private const uint BindingInputTexture = 0;
    private const uint BindingHistoryTexture = 1;
    private const uint BindingVelocityTexture = 2;
    private const uint BindingDepthTexture = 3;
    private const uint BindingParameters = 4;

    private static readonly Vector2[] HaltonSequence =
    [
        new(0.5f, 0.3333f),
        new(0.25f, 0.6667f),
        new(0.75f, 0.1111f),
        new(0.125f, 0.4444f),
        new(0.625f, 0.7778f),
        new(0.375f, 0.2222f),
        new(0.875f, 0.5556f),
        new(0.0625f, 0.8889f)
    ];

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
    private IResource? _historyTexture;
    private IShaderProgram? _shaderProgram;
    private uint _currentWidth;
    private uint _currentHeight;
    private int _frameIndex;
    private bool _historyInvalidated;

    #endregion

    #region 属性

    /// <summary>
    /// 历史帧混合权重（0~1），值越大当前帧权重越高
    /// </summary>
    public float BlendFactor { get; set; }

    /// <summary>
    /// TAA 质量等级
    /// </summary>
    public TaaQuality Quality { get; set; }

    /// <summary>
    /// 是否启用方差裁剪
    /// </summary>
    public bool EnableVarianceClipping { get; set; }

    /// <summary>
    /// 方差裁剪的 Gamma 值
    /// </summary>
    public float VarianceClipGamma { get; set; }

    /// <summary>
    /// 场景速度纹理，需在外部设置
    /// </summary>
    public IResource? VelocityTexture { get; set; }

    /// <summary>
    /// 场景深度纹理，需在外部设置
    /// </summary>
    public IResource? DepthTexture { get; set; }

    /// <summary>
    /// 当前帧的子像素抖动偏移
    /// </summary>
    public Vector2 JitterOffset => HaltonSequence[_frameIndex % HaltonSequence.Length];

    /// <summary>
    /// 关联的着色器程序，需在外部设置后调用 RebuildPipeline
    /// </summary>
    public IShaderProgram? ShaderProgram
    {
        get => _shaderProgram;
        set => _shaderProgram = value;
    }

    /// <summary>
    /// 历史帧纹理
    /// </summary>
    public IResource? HistoryTexture => _historyTexture;

    #endregion

    #region 构造函数

    public TaaEffect() : base("TAA", -5)
    {
        BlendFactor = 0.1f;
        Quality = TaaQuality.Medium;
        EnableVarianceClipping = true;
        VarianceClipGamma = 1.0f;
        _frameIndex = 0;
        _historyInvalidated = true;
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
        _historyTexture?.Dispose();

        _renderPass = null;
        _framebuffer = null;
        _pipelineState = null;
        _descriptorSet = null;
        _vertexBuffer = null;
        _indexBuffer = null;
        _sampler = null;
        _parameterBuffer = null;
        _historyTexture = null;

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
            CreateHistoryTexture(width, height);
            CreateRenderPassAndFramebuffer(width, height);
            _historyInvalidated = true;
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

        _frameIndex++;
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

    /// <summary>
    /// 使历史帧失效，在相机跳变或场景切换时调用
    /// </summary>
    public void InvalidateHistory()
    {
        _historyInvalidated = true;
    }

    /// <summary>
    /// 重置帧索引
    /// </summary>
    public void ResetFrameIndex()
    {
        _frameIndex = 0;
        _historyInvalidated = true;
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

    private void CreateHistoryTexture(uint width, uint height)
    {
        if (Device is null)
        {
            return;
        }

        _historyTexture?.Dispose();

        _historyTexture = Device.CreateTexture(new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = width,
            Height = height,
            Depth = 1,
            Format = ResourceFormat.R16G16B16A16Float,
            Usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource,
            MipLevels = 1,
            ArrayLayers = 1,
            SampleCount = 1
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
                Binding = BindingHistoryTexture,
                DescriptorType = DescriptorType.CombinedImageSampler,
                StageFlags = ShaderStageFlag.Fragment
            },
            new DescriptorSetBinding
            {
                Binding = BindingVelocityTexture,
                DescriptorType = DescriptorType.CombinedImageSampler,
                StageFlags = ShaderStageFlag.Fragment
            },
            new DescriptorSetBinding
            {
                Binding = BindingDepthTexture,
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

        var jitter = JitterOffset;
        var effectiveBlend = _historyInvalidated ? 1.0f : BlendFactor;

        var parameters = new TaaParameters
        {
            BlendFactor = effectiveBlend,
            VarianceClipGamma = VarianceClipGamma,
            JitterX = jitter.X,
            JitterY = jitter.Y,
            EnableVarianceClipping = EnableVarianceClipping ? 1 : 0,
            HistoryInvalidated = _historyInvalidated ? 1 : 0,
            Padding0 = 0,
            Padding1 = 0
        };

        _descriptorSet.BindBuffer(BindingParameters, _parameterBuffer);

        _historyInvalidated = false;
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

        if (_historyTexture is not null)
        {
            _descriptorSet.BindTexture(BindingHistoryTexture, _historyTexture);

            if (_sampler is not null)
            {
                _descriptorSet.BindSampler(BindingHistoryTexture, _sampler);
            }
        }

        if (VelocityTexture is not null)
        {
            _descriptorSet.BindTexture(BindingVelocityTexture, VelocityTexture);

            if (_sampler is not null)
            {
                _descriptorSet.BindSampler(BindingVelocityTexture, _sampler);
            }
        }

        if (DepthTexture is not null)
        {
            _descriptorSet.BindTexture(BindingDepthTexture, DepthTexture);

            if (_sampler is not null)
            {
                _descriptorSet.BindSampler(BindingDepthTexture, _sampler);
            }
        }
    }

    #endregion

    #region 嵌套类型

    private struct TaaParameters
    {
        public float BlendFactor;
        public float VarianceClipGamma;
        public float JitterX;
        public float JitterY;
        public int EnableVarianceClipping;
        public int HistoryInvalidated;
        public int Padding0;
        public int Padding1;
    }

    #endregion
}

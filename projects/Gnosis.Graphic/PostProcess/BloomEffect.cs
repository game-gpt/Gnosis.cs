using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.PostProcess;

/// <summary>
/// 泛光后处理效果
/// 实现多 Pass 降采样 + 升采样泛光管线：
/// 1. 亮度提取 Pass（阈值过滤）
/// 2. 迭代降采样 Pass（高斯模糊）
/// 3. 迭代升采样 Pass（加法混合）
/// 4. 最终合成 Pass（泛光 + 原图混合）
/// </summary>
public sealed class BloomEffect : PostProcessEffect
{
    #region 常量

    private const uint BindingInputTexture = 0;
    private const uint BindingParameters = 1;
    private const int MaxMipLevels = 8;
    private const uint WorkGroupSize = 64;

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

    private readonly IResource?[] _mipTextures = new IResource?[MaxMipLevels];
    private int _mipCount;

    #endregion

    #region 属性

    /// <summary>
    /// 泛光强度
    /// </summary>
    public float Intensity { get; set; }

    /// <summary>
    /// 亮度阈值，高于此值的像素产生泛光
    /// </summary>
    public float Threshold { get; set; }

    /// <summary>
    /// 软阈值，控制阈值过渡的柔和程度
    /// </summary>
    public float SoftThreshold { get; set; }

    /// <summary>
    /// 散射值，控制泛光扩散范围（0~1）
    /// </summary>
    public float Scatter { get; set; }

    /// <summary>
    /// 最大降采样迭代次数
    /// </summary>
    public int MaxIterations { get; set; }

    /// <summary>
    /// 降采样缩放比例
    /// </summary>
    public float DownsampleScale { get; set; }

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

    public BloomEffect() : base("Bloom", 10)
    {
        Intensity = 0.5f;
        Threshold = 1.0f;
        SoftThreshold = 0.5f;
        Scatter = 0.7f;
        MaxIterations = 6;
        DownsampleScale = 2.0f;
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

        foreach (var mip in _mipTextures)
        {
            mip?.Dispose();
        }

        _renderPass = null;
        _framebuffer = null;
        _pipelineState = null;
        _descriptorSet = null;
        _vertexBuffer = null;
        _indexBuffer = null;
        _sampler = null;
        _parameterBuffer = null;

        Array.Clear(_mipTextures);

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
            CreateMipChain(width, height);
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
            BlendMode = BlendMode.Additive,
            DepthTest = false,
            DepthWrite = false,
            CullMode = CullMode.None,
            ColorAttachmentCount = 1,
            ColorFormats = [ResourceFormat.R16G16B16A16Float]
        });
    }

    /// <summary>
    /// 获取指定层级的降采样纹理
    /// </summary>
    /// <param name="level">Mip 层级</param>
    public IResource? GetMipTexture(int level)
    {
        if (level < 0 || level >= _mipCount)
        {
            return null;
        }

        return _mipTextures[level];
    }

    /// <summary>
    /// 获取当前 Mip 链层级数
    /// </summary>
    public int MipCount => _mipCount;

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

    private void CreateMipChain(uint width, uint height)
    {
        foreach (var mip in _mipTextures)
        {
            mip?.Dispose();
        }

        Array.Clear(_mipTextures);

        _mipCount = Math.Min(MaxIterations, CalculateMipCount(width, height));

        uint mipWidth = (uint)(width / DownsampleScale);
        uint mipHeight = (uint)(height / DownsampleScale);

        for (int i = 0; i < _mipCount; i++)
        {
            _mipTextures[i] = Device!.CreateTexture(new TextureDesc
            {
                Dimension = TextureDimension.Texture2D,
                Width = Math.Max(1, mipWidth),
                Height = Math.Max(1, mipHeight),
                Depth = 1,
                Format = ResourceFormat.R16G16B16A16Float,
                Usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource,
                MipLevels = 1,
                ArrayLayers = 1,
                SampleCount = 1
            });

            mipWidth = (uint)(mipWidth / DownsampleScale);
            mipHeight = (uint)(mipHeight / DownsampleScale);
        }
    }

    private static int CalculateMipCount(uint width, uint height)
    {
        int count = 0;
        uint w = width;
        uint h = height;

        while (w > 1 && h > 1)
        {
            w = Math.Max(1, w / 2);
            h = Math.Max(1, h / 2);
            count++;
        }

        return count;
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

        var parameters = new BloomParameters
        {
            Intensity = Intensity,
            Threshold = Threshold,
            SoftThreshold = SoftThreshold,
            Scatter = Scatter,
            MipCount = _mipCount,
            Padding0 = 0,
            Padding1 = 0,
            Padding2 = 0
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

    private struct BloomParameters
    {
        public float Intensity;
        public float Threshold;
        public float SoftThreshold;
        public float Scatter;
        public int MipCount;
        public int Padding0;
        public int Padding1;
        public int Padding2;
    }

    #endregion
}

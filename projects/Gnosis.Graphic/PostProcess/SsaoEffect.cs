using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.PostProcess;

/// <summary>
/// SSAO 质量等级
/// </summary>
public enum SsaoQuality
{
    Low = 0,
    Medium = 1,
    High = 2,
    Ultra = 3
}

/// <summary>
/// 屏幕空间环境光遮蔽后处理效果
/// 基于屏幕空间的深度和法线信息计算环境光遮蔽
/// </summary>
public sealed class SsaoEffect : PostProcessEffect
{
    #region 常量

    private const uint BindingInputTexture = 0;
    private const uint BindingDepthTexture = 1;
    private const uint BindingNormalTexture = 2;
    private const uint BindingParameters = 3;
    private const uint BindingKernelBuffer = 4;
    private const uint BindingNoiseTexture = 5;
    private const int MaxKernelSize = 64;
    private const int NoiseTextureSize = 4;

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
    private IResource? _kernelBuffer;
    private IResource? _noiseTexture;
    private IResource? _aoTexture;
    private IShaderProgram? _shaderProgram;
    private uint _currentWidth;
    private uint _currentHeight;

    private readonly Vector3[] _kernel;
    private bool _kernelDirty;

    #endregion

    #region 属性

    /// <summary>
    /// SSAO 采样半径
    /// </summary>
    public float Radius { get; set; }

    /// <summary>
    /// SSAO 偏置值，防止自遮挡
    /// </summary>
    public float Bias { get; set; }

    /// <summary>
    /// 核心采样数量
    /// </summary>
    public int KernelSize { get; set; }

    /// <summary>
    /// SSAO 质量等级
    /// </summary>
    public SsaoQuality Quality { get; set; }

    /// <summary>
    /// AO 强度
    /// </summary>
    public float Intensity { get; set; }

    /// <summary>
    /// 模糊半径
    /// </summary>
    public int BlurRadius { get; set; }

    /// <summary>
    /// 模糊强度
    /// </summary>
    public float BlurSharpness { get; set; }

    /// <summary>
    /// 场景深度纹理，需在外部设置
    /// </summary>
    public IResource? DepthTexture { get; set; }

    /// <summary>
    /// 场景法线纹理，需在外部设置
    /// </summary>
    public IResource? NormalTexture { get; set; }

    /// <summary>
    /// 关联的着色器程序，需在外部设置后调用 RebuildPipeline
    /// </summary>
    public IShaderProgram? ShaderProgram
    {
        get => _shaderProgram;
        set => _shaderProgram = value;
    }

    /// <summary>
    /// AO 输出纹理
    /// </summary>
    public IResource? AoTexture => _aoTexture;

    #endregion

    #region 构造函数

    public SsaoEffect() : base("SSAO", 3)
    {
        Radius = 0.5f;
        Bias = 0.025f;
        KernelSize = 32;
        Quality = SsaoQuality.Medium;
        Intensity = 1.0f;
        BlurRadius = 2;
        BlurSharpness = 10.0f;

        _kernel = new Vector3[MaxKernelSize];
        _kernelDirty = true;
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
        GenerateKernel();
        GenerateNoiseTexture();

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
        _kernelBuffer?.Dispose();
        _noiseTexture?.Dispose();
        _aoTexture?.Dispose();

        _renderPass = null;
        _framebuffer = null;
        _pipelineState = null;
        _descriptorSet = null;
        _vertexBuffer = null;
        _indexBuffer = null;
        _sampler = null;
        _parameterBuffer = null;
        _kernelBuffer = null;
        _noiseTexture = null;
        _aoTexture = null;

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
            CreateAoTexture(width, height);
            CreateRenderPassAndFramebuffer(width, height);
        }

        if (_kernelDirty)
        {
            GenerateKernel();
            _kernelDirty = false;
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

    /// <summary>
    /// 标记核心采样需要重新生成
    /// </summary>
    public void MarkKernelDirty()
    {
        _kernelDirty = true;
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

    private void GenerateKernel()
    {
        if (Device is null)
        {
            return;
        }

        var random = new Random(42);

        for (int i = 0; i < MaxKernelSize; i++)
        {
            var sample = new Vector3(
                (float)(random.NextDouble() * 2.0 - 1.0),
                (float)(random.NextDouble() * 2.0 - 1.0),
                (float)random.NextDouble()
            );
            sample = Vector3.Normalize(sample);
            sample *= (float)random.NextDouble();

            float scale = (float)i / MaxKernelSize;
            scale = 0.1f + (1.0f - 0.1f) * scale * scale;

            _kernel[i] = sample * scale;
        }

        _kernelBuffer?.Dispose();

        _kernelBuffer = Device.CreateBuffer(new BufferDesc
        {
            Size = (ulong)(MaxKernelSize * 3 * sizeof(float)),
            Usage = BufferUsage.StorageBuffer | BufferUsage.TransferDst,
            DeviceLocal = true
        });
    }

    private void GenerateNoiseTexture()
    {
        if (Device is null)
        {
            return;
        }

        var random = new Random(123);
        var noiseData = new float[NoiseTextureSize * NoiseTextureSize * 4];

        for (int i = 0; i < NoiseTextureSize * NoiseTextureSize; i++)
        {
            var noise = new Vector3(
                (float)(random.NextDouble() * 2.0 - 1.0),
                (float)(random.NextDouble() * 2.0 - 1.0),
                0.0f
            );
            noise = Vector3.Normalize(noise);

            noiseData[i * 4 + 0] = noise.X;
            noiseData[i * 4 + 1] = noise.Y;
            noiseData[i * 4 + 2] = noise.Z;
            noiseData[i * 4 + 3] = 0.0f;
        }

        _noiseTexture?.Dispose();

        _noiseTexture = Device.CreateTexture(new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = NoiseTextureSize,
            Height = NoiseTextureSize,
            Depth = 1,
            Format = ResourceFormat.R32G32B32A32Float,
            Usage = TextureUsage.ShaderResource | TextureUsage.TransferDst,
            MipLevels = 1,
            ArrayLayers = 1,
            SampleCount = 1
        });
    }

    private void CreateAoTexture(uint width, uint height)
    {
        if (Device is null)
        {
            return;
        }

        _aoTexture?.Dispose();

        _aoTexture = Device.CreateTexture(new TextureDesc
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
                Binding = BindingDepthTexture,
                DescriptorType = DescriptorType.CombinedImageSampler,
                StageFlags = ShaderStageFlag.Fragment
            },
            new DescriptorSetBinding
            {
                Binding = BindingNormalTexture,
                DescriptorType = DescriptorType.CombinedImageSampler,
                StageFlags = ShaderStageFlag.Fragment
            },
            new DescriptorSetBinding
            {
                Binding = BindingParameters,
                DescriptorType = DescriptorType.UniformBuffer,
                StageFlags = ShaderStageFlag.Fragment
            },
            new DescriptorSetBinding
            {
                Binding = BindingKernelBuffer,
                DescriptorType = DescriptorType.StorageBuffer,
                StageFlags = ShaderStageFlag.Fragment
            },
            new DescriptorSetBinding
            {
                Binding = BindingNoiseTexture,
                DescriptorType = DescriptorType.CombinedImageSampler,
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

        var parameters = new SsaoParameters
        {
            Radius = Radius,
            Bias = Bias,
            Intensity = Intensity,
            KernelSize = KernelSize,
            NoiseSize = NoiseTextureSize,
            BlurRadius = BlurRadius,
            BlurSharpness = BlurSharpness,
            Width = _currentWidth,
            Height = _currentHeight
        };

        _descriptorSet.BindBuffer(BindingParameters, _parameterBuffer);

        if (_kernelBuffer is not null)
        {
            _descriptorSet.BindBuffer(BindingKernelBuffer, _kernelBuffer);
        }
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

        if (DepthTexture is not null)
        {
            _descriptorSet.BindTexture(BindingDepthTexture, DepthTexture);

            if (_sampler is not null)
            {
                _descriptorSet.BindSampler(BindingDepthTexture, _sampler);
            }
        }

        if (NormalTexture is not null)
        {
            _descriptorSet.BindTexture(BindingNormalTexture, NormalTexture);

            if (_sampler is not null)
            {
                _descriptorSet.BindSampler(BindingNormalTexture, _sampler);
            }
        }

        if (_noiseTexture is not null)
        {
            _descriptorSet.BindTexture(BindingNoiseTexture, _noiseTexture);

            if (_sampler is not null)
            {
                _descriptorSet.BindSampler(BindingNoiseTexture, _sampler);
            }
        }
    }

    #endregion

    #region 嵌套类型

    private struct SsaoParameters
    {
        public float Radius;
        public float Bias;
        public float Intensity;
        public int KernelSize;
        public int NoiseSize;
        public int BlurRadius;
        public float BlurSharpness;
        public uint Width;
        public uint Height;
    }

    #endregion
}

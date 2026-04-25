namespace Gnosis.Graphic.RHI;

/// <summary>
///     管线类型
/// </summary>
public enum PipelineType
{
    Graphics = 0,
    Compute = 1
}

/// <summary>
///     管线状态描述，包含完整的渲染管线配置
/// </summary>
public record PipelineStateDesc
{
    public required IShaderProgram Shader { get; init; }
    public IResource[]? ShaderResources { get; init; }
    public PipelineType PipelineType { get; init; } = PipelineType.Graphics;
    public PrimitiveTopology Topology { get; init; } = PrimitiveTopology.TriangleList;
    public BlendMode BlendMode { get; init; }
    public bool DepthTest { get; init; }
    public bool DepthWrite { get; init; } = true;
    public CompareFunction DepthCompare { get; init; } = CompareFunction.Less;
    public CullMode CullMode { get; init; }
    public FrontFace FrontFace { get; init; } = FrontFace.CounterClockwise;
    public PolygonMode PolygonMode { get; init; } = PolygonMode.Fill;
    public float LineWidth { get; init; } = 1.0f;
    public StencilOpState StencilFront { get; init; } = new();
    public StencilOpState StencilBack { get; init; } = new();
    public byte StencilReadMask { get; init; } = 0xFF;
    public byte StencilWriteMask { get; init; } = 0xFF;
    public RenderTargetBlendState[] BlendStates { get; init; } = [];
    public uint ColorAttachmentCount { get; init; }
    public ResourceFormat[] ColorFormats { get; init; } = [];
    public ResourceFormat DepthStencilFormat { get; init; } = ResourceFormat.D24UnormS8Uint;
    public uint SampleCount { get; init; } = 1;
}

/// <summary>
///     管线状态接口
/// </summary>
public interface IPipelineState : IDisposable
{
    /// <summary>
    ///     关联的着色器程序
    /// </summary>
    IShaderProgram Shader { get; }

    /// <summary>
    ///     混合模式
    /// </summary>
    BlendMode BlendMode { get; }

    /// <summary>
    ///     深度测试启用
    /// </summary>
    bool DepthTest { get; }

    /// <summary>
    ///     深度写入启用
    /// </summary>
    bool DepthWrite { get; }

    /// <summary>
    ///     深度比较函数
    /// </summary>
    CompareFunction DepthCompare { get; }

    /// <summary>
    ///     剔除模式
    /// </summary>
    CullMode CullMode { get; }

    /// <summary>
    ///     正面朝向
    /// </summary>
    FrontFace FrontFace { get; }

    /// <summary>
    ///     多边形模式
    /// </summary>
    PolygonMode PolygonMode { get; }

    /// <summary>
    ///     拓扑类型
    /// </summary>
    PrimitiveTopology Topology { get; }
}

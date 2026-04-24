namespace Gnosis.Graphic.RHI.OpenGL;

internal sealed class OpenGLPipelineState : IPipelineState
{
    #region IPipelineState 属性

    public IShaderProgram Shader => _desc.Shader;
    public BlendMode BlendMode => _desc.BlendMode;
    public bool DepthTest => _desc.DepthTest;
    public bool DepthWrite => _desc.DepthWrite;
    public CompareFunction DepthCompare => _desc.DepthCompare;
    public CullMode CullMode => _desc.CullMode;
    public FrontFace FrontFace => _desc.FrontFace;
    public PolygonMode PolygonMode => _desc.PolygonMode;
    public PrimitiveTopology Topology => _desc.Topology;

    #endregion

    #region 内部状态

    private readonly PipelineStateDesc _desc;
    private bool _isDisposed;

    #endregion

    #region 构造函数

    public OpenGLPipelineState(in PipelineStateDesc desc)
    {
        _desc = desc;
    }

    #endregion

    #region 公开方法

    public void Apply()
    {
        if (_desc.ShaderResources is { Length: > 0 })
        {
            var shaderRes = _desc.ShaderResources[0] as OpenGLResource;
            if (shaderRes?.GlProgram != 0)
            {
                GlNative.UseProgram!(shaderRes.GlProgram);
            }
        }

        if (BlendMode != BlendMode.None)
        {
            GlNative.Enable!(GlConstants.GL_BLEND);
            ApplyBlendState();
        }
        else
        {
            GlNative.Disable!(GlConstants.GL_BLEND);
        }

        if (DepthTest)
        {
            GlNative.Enable!(GlConstants.GL_DEPTH_TEST);
            GlNative.DepthFunc!(GlConversions.ToGlCompareFunction(DepthCompare));
            GlNative.DepthMask!(DepthWrite);
        }
        else
        {
            GlNative.Disable!(GlConstants.GL_DEPTH_TEST);
        }

        if (CullMode != CullMode.None)
        {
            GlNative.Enable!(GlConstants.GL_CULL_FACE);
            GlNative.CullFace!(GlConversions.ToGlCullMode(CullMode));
            GlNative.FrontFace!(GlConversions.ToGlFrontFace(FrontFace));
        }
        else
        {
            GlNative.Disable!(GlConstants.GL_CULL_FACE);
        }

        GlNative.PolygonMode!(GlConstants.GL_FRONT_AND_BACK, GlConversions.ToGlPolygonMode(PolygonMode));
    }

    #endregion

    #region 私有方法

    private void ApplyBlendState()
    {
        if (_desc.BlendStates.Length > 0)
        {
            var bs = _desc.BlendStates[0];
            if (bs.BlendEnable)
            {
                GlNative.BlendFuncSeparate!(
                    GlConversions.ToGlBlendFactor(bs.SrcBlend),
                    GlConversions.ToGlBlendFactor(bs.DstBlend),
                    GlConversions.ToGlBlendFactor(bs.SrcAlphaBlend),
                    GlConversions.ToGlBlendFactor(bs.DstAlphaBlend));
            }
        }
        else
        {
            ApplyBlendMode(BlendMode);
        }
    }

    private static void ApplyBlendMode(BlendMode mode)
    {
        switch (mode)
        {
            case BlendMode.AlphaBlend:
                GlNative.BlendFunc!(GlConstants.GL_SRC_ALPHA, GlConstants.GL_ONE_MINUS_SRC_ALPHA);
                break;
            case BlendMode.Additive:
                GlNative.BlendFunc!(GlConstants.GL_SRC_ALPHA, GlConstants.GL_ONE);
                break;
            case BlendMode.Multiply:
                GlNative.BlendFunc!(GlConstants.GL_DST_COLOR, GlConstants.GL_ZERO);
                break;
            case BlendMode.Premultiplied:
                GlNative.BlendFunc!(GlConstants.GL_ONE, GlConstants.GL_ONE_MINUS_SRC_ALPHA);
                break;
            default:
                GlNative.BlendFunc!(GlConstants.GL_SRC_ALPHA, GlConstants.GL_ONE_MINUS_SRC_ALPHA);
                break;
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _isDisposed = true;
    }

    #endregion
}

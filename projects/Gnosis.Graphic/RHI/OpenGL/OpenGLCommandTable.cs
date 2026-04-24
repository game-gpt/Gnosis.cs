namespace Gnosis.Graphic.RHI.OpenGL;

internal sealed unsafe class OpenGLCommandTable : ICommandTable
{
    #region 内部状态

    private bool _isRecording;
    private bool _isInRenderPass;
    private OpenGLPipelineState? _currentPipeline;

    #endregion

    #region 命令录制生命周期

    public void Begin()
    {
        _isRecording = true;
        _currentPipeline = null;
    }

    public void End()
    {
        _isRecording = false;
    }

    #endregion

    #region 渲染通道

    public void BeginRenderPass(IRhiRenderPass renderPass, IRhiFramebuffer framebuffer, IReadOnlyList<(float r, float g, float b, float a)>? clearColors = null, float clearDepth = 1.0f, byte clearStencil = 0)
    {
        _isInRenderPass = true;

        var fb = framebuffer as OpenGLFramebuffer;
        GlNative.BindFramebuffer!(GlConstants.GL_FRAMEBUFFER, fb?.GlFramebuffer ?? 0);
        GlNative.Viewport!(0, 0, (int)(fb?.Width ?? 0), (int)(fb?.Height ?? 0));

        uint clearMask = 0;
        if (clearColors != null)
        {
            for (int i = 0; i < clearColors.Count; i++)
            {
                var c = clearColors[i];
                GlNative.ClearColor!(c.r, c.g, c.b, c.a);
                clearMask |= GlConstants.GL_COLOR_BUFFER_BIT;
            }
        }

        clearMask |= GlConstants.GL_DEPTH_BUFFER_BIT | GlConstants.GL_STENCIL_BUFFER_BIT;

        if (clearMask != 0)
        {
            GlNative.Clear!(clearMask);
        }
    }

    public void EndRenderPass()
    {
        _isInRenderPass = false;
        GlNative.BindFramebuffer!(GlConstants.GL_FRAMEBUFFER, 0);
    }

    #endregion

    #region 管线状态

    public void SetPipelineState(IPipelineState pipelineState)
    {
        _currentPipeline = pipelineState as OpenGLPipelineState;
        _currentPipeline?.Apply();
    }

    #endregion

    #region 视口和裁剪

    public void SetViewport(float x, float y, float width, float height, float minDepth = 0.0f, float maxDepth = 1.0f)
    {
        GlNative.Viewport!((int)x, (int)y, (int)width, (int)height);
    }

    public void SetScissor(int x, int y, uint width, uint height)
    {
        GlNative.Enable!(GlConstants.GL_SCISSOR_TEST);
        GlNative.Scissor!(x, y, (int)width, (int)height);
    }

    #endregion

    #region 顶点和索引缓冲

    public void SetVertexBuffer(IResource buffer, ulong offset = 0)
    {
        var glBuffer = buffer as OpenGLResource;
        if (glBuffer != null)
        {
            GlNative.BindBuffer!(GlConstants.GL_ARRAY_BUFFER, glBuffer.GlBuffer);
        }
    }

    public void SetIndexBuffer(IResource buffer, ulong offset = 0)
    {
        var glBuffer = buffer as OpenGLResource;
        if (glBuffer != null)
        {
            GlNative.BindBuffer!(GlConstants.GL_ELEMENT_ARRAY_BUFFER, glBuffer.GlBuffer);
        }
    }

    #endregion

    #region 清除

    public void ClearRenderTarget(uint attachmentIndex, float r, float g, float b, float a)
    {
        GlNative.ClearColor!(r, g, b, a);
        GlNative.Clear!(GlConstants.GL_COLOR_BUFFER_BIT);
    }

    public void ClearDepthStencil(float depth, byte stencil)
    {
        GlNative.Clear!(GlConstants.GL_DEPTH_BUFFER_BIT | GlConstants.GL_STENCIL_BUFFER_BIT);
    }

    #endregion

    #region 描述符集绑定

    public void BindDescriptorSet(IRhiDescriptorSet descriptorSet, uint setIndex)
    {
        var glDescSet = descriptorSet as OpenGLDescriptorSet;
        glDescSet?.Bind();
    }

    #endregion

    #region 绘制命令

    public void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
    {
        var topology = _currentPipeline?.Topology ?? PrimitiveTopology.TriangleList;
        GlNative.DrawArrays!(GlConversions.ToGlPrimitiveTopology(topology), (int)firstVertex, (int)vertexCount);
    }

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
    {
        var topology = _currentPipeline?.Topology ?? PrimitiveTopology.TriangleList;
        nint offset = (nint)(firstIndex * sizeof(uint));
        GlNative.DrawElements!(GlConversions.ToGlPrimitiveTopology(topology), (int)indexCount, GlConstants.GL_UNSIGNED_INT, offset);
    }

    public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
    {
    }

    #endregion

    #region 资源屏障

    public void PipelineBarrier(PipelineStageFlag srcStage, PipelineStageFlag dstStage, AccessFlag srcAccess, AccessFlag dstAccess)
    {
        GlNative.Flush!();
    }

    public void CopyResource(IResource src, IResource dst)
    {
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
    }

    #endregion
}

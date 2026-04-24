namespace Gnosis.Graphic.RHI.OpenGL;

internal sealed unsafe class OpenGLFramebuffer : IRhiFramebuffer
{
    public uint GlFramebuffer { get; }
    public uint Width { get; }
    public uint Height { get; }
    public IReadOnlyList<IResource> Attachments { get; }
    public IRhiRenderPass RenderPass { get; }

    private bool _isDisposed;

    public OpenGLFramebuffer(uint glFramebuffer, uint width, uint height,
        IReadOnlyList<IResource> attachments, IRhiRenderPass renderPass)
    {
        GlFramebuffer = glFramebuffer;
        Width = width;
        Height = height;
        Attachments = attachments;
        RenderPass = renderPass;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (GlFramebuffer != 0)
        {
            uint fb = GlFramebuffer;
            GlNative.DeleteFramebuffers!(1, &fb);
        }
    }
}

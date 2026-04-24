namespace Gnosis.Graphic.RHI.OpenGL;

internal sealed class OpenGLRenderPass : IRhiRenderPass
{
    public uint AttachmentCount { get; }
    public uint SubPassCount { get; }

    private bool _isDisposed;

    public OpenGLRenderPass(uint attachmentCount, uint subPassCount)
    {
        AttachmentCount = attachmentCount;
        SubPassCount = subPassCount;
    }

    public void Dispose()
    {
        _isDisposed = true;
    }
}

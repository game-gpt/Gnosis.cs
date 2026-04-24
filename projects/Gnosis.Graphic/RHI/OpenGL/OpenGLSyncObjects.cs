namespace Gnosis.Graphic.RHI.OpenGL;

internal sealed class OpenGLFence : IRhiFence
{
    public bool IsSignaled { get; private set; }

    private bool _isDisposed;

    public void Wait(ulong timeout = ulong.MaxValue)
    {
        GlNative.Finish!();
        IsSignaled = true;
    }

    public void Reset()
    {
        IsSignaled = false;
    }

    public void Dispose()
    {
        _isDisposed = true;
    }
}

internal sealed class OpenGLSemaphore : IRhiSemaphore
{
    private bool _isDisposed;

    public void Dispose()
    {
        _isDisposed = true;
    }
}

namespace Gnosis.Graphic.RHI.OpenGL;

internal sealed class OpenGLSwapchain : IRhiSwapchain
{
    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public ResourceFormat Format => ResourceFormat.R8G8B8A8Unorm;
    public uint ImageCount => 2;

    private nint _windowHandle;
    private nint _deviceContext;
    private nint _glContext;
    private bool _isDisposed;

    public OpenGLSwapchain(nint windowHandle, nint deviceContext, nint glContext, uint width, uint height)
    {
        _windowHandle = windowHandle;
        _deviceContext = deviceContext;
        _glContext = glContext;
        Width = width;
        Height = height;
    }

    public uint AcquireNextImage(IRhiSemaphore? semaphore, IRhiFence? fence)
    {
        return 0;
    }

    public void Present(IReadOnlyList<IRhiSemaphore> waitSemaphores)
    {
        GlNative.Flush!();
    }

    public void Resize(uint width, uint height)
    {
        Width = width;
        Height = height;
        GlNative.Viewport!(0, 0, (int)width, (int)height);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_glContext != nint.Zero)
        {
            GlNative.wglDeleteContext(_glContext);
            _glContext = nint.Zero;
        }
    }
}

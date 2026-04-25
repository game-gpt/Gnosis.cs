using Gnosis.Platform.Window.GL;

namespace Gnosis.Platform.Window.Android;

public sealed class AndroidWindow : IPlatformWindow
{
    public nint Handle { get; private set; }
    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public bool IsClosing { get; private set; }
    public bool IsMinimized { get; private set; }
    public string Title { get; set; } = "";
    public GLContext? GL { get; private set; }

    public event Action<uint, uint>? OnResize;
    public event Action? OnClosing;

    public void PollEvents() => throw new PlatformNotSupportedException("Android EGL 后端待实现");
    public void MakeCurrent() => throw new PlatformNotSupportedException("Android EGL 后端待实现");
    public void SwapBuffers() => throw new PlatformNotSupportedException("Android EGL 后端待实现");
    public nint GetProcAddress(string name) => throw new PlatformNotSupportedException("Android EGL 后端待实现");

    public static AndroidWindow Create(WindowCreateInfo info)
    {
        throw new PlatformNotSupportedException(
            "Android EGL 后端待实现\n" +
            "实现路径:\n" +
            "  1. ANativeWindow_fromSurface → 获取原生窗口\n" +
            "  2. eglGetDisplay + eglInitialize → 初始化 EGL\n" +
            "  3. eglCreateContext → 创建 OpenGL ES 上下文\n" +
            "  4. eglMakeCurrent → 绑定上下文\n" +
            "  5. eglGetProcAddress → 加载 GL 函数指针\n" +
            "  6. eglSwapBuffers → 交换缓冲区");
    }

    public void Dispose() { }
}

using Gnosis.Platform.Window.GL;

namespace Gnosis.Platform.Window.Linux;

public sealed class X11Window : IPlatformWindow
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

    public void PollEvents() => throw new PlatformNotSupportedException("Linux X11 后端待实现");
    public void MakeCurrent() => throw new PlatformNotSupportedException("Linux GLX 后端待实现");
    public void SwapBuffers() => throw new PlatformNotSupportedException("Linux GLX 后端待实现");
    public nint GetProcAddress(string name) => throw new PlatformNotSupportedException("Linux GLX 后端待实现");

    public static X11Window Create(WindowCreateInfo info)
    {
        throw new PlatformNotSupportedException(
            "Linux X11 + GLX 后端待实现\n" +
            "实现路径:\n" +
            "  1. XOpenDisplay → 获取 Display 连接\n" +
            "  2. XCreateWindow → 创建 X11 窗口\n" +
            "  3. glXCreateContextAttribsARB → 创建 OpenGL 上下文\n" +
            "  4. glXMakeCurrent → 绑定上下文\n" +
            "  5. glXGetProcAddress → 加载 GL 函数指针\n" +
            "  6. glXSwapBuffers → 交换缓冲区");
    }

    public void Dispose() { }
}

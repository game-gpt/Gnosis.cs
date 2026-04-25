using Gnosis.Platform.GL;

namespace Gnosis.Platform.MacOS;

public sealed unsafe class CocoaWindow : IPlatformWindow
{
    #region IPlatformWindow 属性

    public nint Handle { get; private set; }
    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public bool IsClosing { get; private set; }
    public bool IsMinimized { get; private set; }
    public string Title { get; set; } = "";
    public GLContext? GL { get; private set; }

    #endregion

    #region IPlatformWindow 事件

    public event Action<uint, uint>? OnResize;
    public event Action? OnClosing;

    #endregion

    #region IPlatformWindow 方法

    public void PollEvents() => throw new PlatformNotSupportedException("macOS Cocoa 后端待实现");

    public void MakeCurrent() => throw new PlatformNotSupportedException("macOS NSOpenGL 后端待实现");

    public void SwapBuffers() => throw new PlatformNotSupportedException("macOS NSOpenGL 后端待实现");

    public nint GetProcAddress(string name) => throw new PlatformNotSupportedException("macOS NSOpenGL 后端待实现");

    public static CocoaWindow Create(WindowCreateInfo info)
    {
        throw new PlatformNotSupportedException(
            "macOS Cocoa + NSOpenGL 后端待实现\n" +
            "实现路径:\n" +
            "  1. NSApplication.sharedApplication → 初始化应用\n" +
            "  2. NSWindow → 创建 Cocoa 窗口\n" +
            "  3. NSOpenGLContext + NSOpenGLPixelFormat → 创建 OpenGL 上下文\n" +
            "  4. makeCurrentContext → 绑定上下文\n" +
            "  5. CGLGetProcAddress → 加载 GL 函数指针\n" +
            "  6. flushBuffer → 交换缓冲区");
    }

    #endregion

    #region IDisposable

    public void Dispose() { }

    #endregion
}

using Gnosis.Platform.GL;

namespace Gnosis.Platform.iOS;

public sealed unsafe class IosWindow : IPlatformWindow
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

    public void PollEvents() => throw new PlatformNotSupportedException("iOS EGL 后端待实现");

    public void MakeCurrent() => throw new PlatformNotSupportedException("iOS EGL 后端待实现");

    public void SwapBuffers() => throw new PlatformNotSupportedException("iOS EGL 后端待实现");

    public nint GetProcAddress(string name) => throw new PlatformNotSupportedException("iOS EGL 后端待实现");

    public static IosWindow Create(WindowCreateInfo info)
    {
        throw new PlatformNotSupportedException(
            "iOS EGL + UIKit 后端待实现\n" +
            "实现路径:\n" +
            "  1. UIView + CAEAGLLayer → 创建渲染层\n" +
            "  2. EAGLContext + EAGLSharegroup → 创建 OpenGL ES 上下文\n" +
            "  3. eglMakeCurrent → 绑定上下文\n" +
            "  4. eglGetProcAddress → 加载 GL 函数指针\n" +
            "  5. presentRenderbuffer → 交换缓冲区");
    }

    #endregion

    #region IDisposable

    public void Dispose() { }

    #endregion
}

using Gnosis.Platform.Window.GL;

namespace Gnosis.Platform.Window.Web;

public sealed class WebWindow : IPlatformWindow
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

    public void PollEvents() => throw new PlatformNotSupportedException("WebAssembly WebGL 后端待实现");
    public void MakeCurrent() => throw new PlatformNotSupportedException("WebAssembly WebGL 后端待实现");
    public void SwapBuffers() => throw new PlatformNotSupportedException("WebAssembly WebGL 后端待实现");
    public nint GetProcAddress(string name) => throw new PlatformNotSupportedException("WebAssembly WebGL 后端待实现");

    public static WebWindow Create(WindowCreateInfo info)
    {
        throw new PlatformNotSupportedException(
            "WebAssembly WebGL 后端待实现\n" +
            "实现路径:\n" +
            "  1. JS Interop → 获取 canvas 元素\n" +
            "  2. canvas.getContext('webgl2') → 创建 WebGL2 上下文\n" +
            "  3. WebGL API 直接通过 JS 调用（无需 GetProcAddress）\n" +
            "  4. requestAnimationFrame → 驱动渲染循环\n" +
            "  5. canvas.toDataURL → 截图功能");
    }

    public void Dispose() { }
}

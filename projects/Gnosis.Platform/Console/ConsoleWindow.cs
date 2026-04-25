using Gnosis.Platform.GL;

namespace Gnosis.Platform.Handheld;

public sealed unsafe class ConsoleWindow : IPlatformWindow
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

    public void PollEvents() => throw new PlatformNotSupportedException("掌机后端待实现");

    public void MakeCurrent() => throw new PlatformNotSupportedException("掌机后端待实现");

    public void SwapBuffers() => throw new PlatformNotSupportedException("掌机后端待实现");

    public nint GetProcAddress(string name) => throw new PlatformNotSupportedException("掌机后端待实现");

    public static ConsoleWindow Create(WindowCreateInfo info)
    {
        throw new PlatformNotSupportedException(
            "掌机后端待实现\n" +
            "支持平台:\n" +
            "  - PlayStation: 使用平台 SDK 提供的 GXM/Graphics API\n" +
            "  - Xbox: 使用 DirectX 12 或平台特定 API\n" +
            "  - Switch: 使用 NVN 或 OpenGL ES + EGL\n" +
            "各平台需要 NDA SDK，实现时需联系平台方获取开发套件");
    }

    #endregion

    #region IDisposable

    public void Dispose() { }

    #endregion
}

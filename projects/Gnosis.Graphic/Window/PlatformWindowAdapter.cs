using Gnosis.Platform.Window;
using Gnosis.Platform.Window.GL;

namespace Gnosis.Graphic.Window;

public sealed class PlatformWindowAdapter : IWindow
{
    #region 字段

    private readonly IPlatformWindow _platformWindow;

    #endregion

    #region 属性

    public nint WindowHandle => _platformWindow.Handle;
    public uint Width => _platformWindow.Width;
    public uint Height => _platformWindow.Height;
    public bool IsClosing => _platformWindow.IsClosing;
    public bool IsMinimized => _platformWindow.IsMinimized;

    public string Title
    {
        get => _platformWindow.Title;
        set => _platformWindow.Title = value;
    }

    public object? GlContext => _platformWindow.GL;

    #endregion

    #region 事件

    public event Action<EventArgs>? OnResize;
    public event Action<EventArgs>? OnClosing;

    #endregion

    #region 构造函数

    private PlatformWindowAdapter(IPlatformWindow platformWindow)
    {
        _platformWindow = platformWindow;
        _platformWindow.OnResize += (w, h) => OnResize?.Invoke(EventArgs.Empty);
        _platformWindow.OnClosing += () => OnClosing?.Invoke(EventArgs.Empty);
    }

    #endregion

    #region IWindow 实现

    public void PollEvents() => _platformWindow.PollEvents();

    public void MakeCurrent() => _platformWindow.MakeCurrent();

    public void SwapBuffers() => _platformWindow.SwapBuffers();

    public static IWindow Create(WindowOptions options)
    {
        var createInfo = new WindowCreateInfo
        {
            Title = options.Title,
            Width = options.Width,
            Height = options.Height,
            VSync = options.VSync,
            Fullscreen = options.Fullscreen,
            Resizable = options.Resizable,
            Visible = options.Visible
        };

        var platformWindow = PlatformWindowFactory.Create(createInfo);
        return new PlatformWindowAdapter(platformWindow);
    }

    #endregion

    #region 内部方法

    public IPlatformWindow GetPlatformWindow() => _platformWindow;

    public nint GetProcAddress(string name) => _platformWindow.GetProcAddress(name);

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _platformWindow.Dispose();
    }

    #endregion
}

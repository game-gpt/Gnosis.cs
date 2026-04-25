namespace Gnosis.Graphic.Window;

public interface IWindow : IDisposable
{
    nint WindowHandle { get; }
    uint Width { get; }
    uint Height { get; }
    bool IsClosing { get; }
    bool IsMinimized { get; }
    string Title { get; set; }

    object? GlContext { get; }

    event Action<EventArgs>? OnResize;
    event Action<EventArgs>? OnClosing;

    void PollEvents();
    void MakeCurrent();
    void SwapBuffers();

    static abstract IWindow Create(WindowOptions options);
}

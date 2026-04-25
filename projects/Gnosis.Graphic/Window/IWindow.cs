namespace Gnosis.Graphic.Window;

public interface IWindow : IDisposable
{
    nint WindowHandle { get; }
    uint Width { get; }
    uint Height { get; }
    bool IsClosing { get; }
    bool IsMinimized { get; }
    string Title { get; set; }

    event Action<EventArgs>? OnResize;
    event Action<EventArgs>? OnClosing;

    void PollEvents();

    static abstract IWindow Create(WindowOptions options);
}

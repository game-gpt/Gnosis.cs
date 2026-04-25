using Gnosis.Platform.Window.GL;

namespace Gnosis.Platform.Window;

public interface IPlatformWindow : IDisposable
{
    nint Handle { get; }
    uint Width { get; }
    uint Height { get; }
    bool IsClosing { get; }
    bool IsMinimized { get; }
    string Title { get; set; }
    GLContext? GL { get; }

    void PollEvents();
    void MakeCurrent();
    void SwapBuffers();

    nint GetProcAddress(string name);

    event Action<uint, uint>? OnResize;
    event Action? OnClosing;
}

using Gnosis.Rendering.RHI;

namespace Gnosis.Rendering.Pipeline;

public record RenderContext
{
    public required IView View { get; init; }
    public float DeltaTime { get; init; }
    public ulong FrameIndex { get; init; }
}

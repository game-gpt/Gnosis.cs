using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Pipeline;

public record RenderContext
{
    public required IView View { get; init; }
    public IDevice? Device { get; init; }
    public float DeltaTime { get; init; }
    public ulong FrameIndex { get; init; }
    public uint Width { get; init; }
    public uint Height { get; init; }
}

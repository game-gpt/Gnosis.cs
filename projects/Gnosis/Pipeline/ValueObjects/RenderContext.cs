namespace GnosisEngine.Pipeline.ValueObjects;

public record RenderContext
{
    public required GnosisEngine.RHI.IView View { get; init; }
    public float DeltaTime { get; init; }
    public ulong FrameIndex { get; init; }
}

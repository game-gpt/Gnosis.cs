namespace Gnosis.Input.Touch;

public struct TouchPoint
{
    public int FingerId { get; init; }
    public float[] Position { get; init; }
    public float[] DeltaPosition { get; init; }
    public TouchPhase Phase { get; init; }
    public float Pressure { get; init; }
}
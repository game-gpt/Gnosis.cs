namespace Gnosis.Input.Interface;

public interface ITouch : IInputDevice
{
    int TouchCount { get; }
    TouchPoint GetTouch(int index);
}

public struct TouchPoint
{
    public int FingerId { get; init; }
    public float[] Position { get; init; }
    public float[] DeltaPosition { get; init; }
    public TouchPhase Phase { get; init; }
    public float Pressure { get; init; }
}

public enum TouchPhase
{
    Began = 0,
    Moved = 1,
    Stationary = 2,
    Ended = 3,
    Canceled = 4
}

namespace Gnosis.Input;

public interface IInputContext
{
    IInputAction Action { get; }
    IInputDevice Device { get; }
    float Value { get; }
    bool IsPressed { get; }
    float Duration { get; }
    float StartTime { get; }
}

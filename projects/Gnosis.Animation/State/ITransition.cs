namespace Gnosis.Animation.State;

public interface ITransition
{
    string SourceState { get; }
    string DestinationState { get; }
    float Duration { get; set; }
    float ExitTime { get; set; }
    bool HasExitTime { get; set; }
    string Condition { get; set; }
}

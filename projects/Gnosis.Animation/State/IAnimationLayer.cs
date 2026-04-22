namespace Gnosis.Animation.State;

public interface IAnimationLayer
{
    string Name { get; }
    float Weight { get; set; }
    IAnimationStateMachine StateMachine { get; }
    string? MaskName { get; set; }
    bool IsAdditive { get; set; }
    bool IsEnabled { get; set; }
}

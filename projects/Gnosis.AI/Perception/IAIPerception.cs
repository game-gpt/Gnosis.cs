namespace Gnosis.AI.Perception;

public interface IAIPerception
{
    IReadOnlyList<IAISenseConfig> SenseConfigs { get; }
    IReadOnlyList<IAIStimulusSource> PerceivedTargets { get; }
    void AddSense(IAISenseConfig config);
    void RemoveSense(AISenseType senseType);
    void Update(float delta);
    bool IsTargetPerceived(IAIStimulusSource target);
    float[] GetLastKnownPosition(IAIStimulusSource target);
}

using Gnosis.Core.Math;

namespace Gnosis.AI.Perception;

public interface IAIPerception
{
    IReadOnlyList<IAISenseConfig> SenseConfigs { get; }
    IReadOnlyList<IAIStimulusSource> PerceivedTargets { get; }
    IPerceptionMemory Memory { get; }
    void AddSense(IAISenseConfig config);
    void RemoveSense(AISenseType senseType);
    void Update(float delta);
    bool IsTargetPerceived(IAIStimulusSource target);
    Vector3 GetLastKnownPosition(IAIStimulusSource target);
}

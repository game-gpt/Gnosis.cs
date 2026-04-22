namespace Gnosis.AI.State;

public interface ITargetSelector
{
    float[]? CurrentTarget { get; }
    float[]? CurrentTargetPosition { get; }
    void SetTarget(float[] position);
    void ClearTarget();
    bool HasTarget { get; }
    float TargetDistance { get; }
    void Update(float delta);
}

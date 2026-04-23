using Gnosis.Core.Math;

namespace Gnosis.AI.State;

public interface ITargetSelector
{
    Vector3? CurrentTarget { get; }
    Vector3? CurrentTargetPosition { get; }
    void SetTarget(Vector3 position);
    void ClearTarget();
    bool HasTarget { get; }
    float TargetDistance { get; }
    void Update(float delta);
}

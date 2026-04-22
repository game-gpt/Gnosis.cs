using Gnosis.AI.Behavior;
using Gnosis.AI.Perception;

namespace Gnosis.AI.State;

public interface IAIController
{
    IBehaviorTree? BehaviorTree { get; }
    IAIPerception? Perception { get; }
    ITargetSelector TargetSelector { get; }
    void SetBehaviorTree(IBehaviorTree tree);
    void SetPerception(IAIPerception perception);
    void Update(float delta);
    void Reset();
}

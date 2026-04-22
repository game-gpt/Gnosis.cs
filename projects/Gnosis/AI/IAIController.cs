using Gnosis.AI.BehaviorTree;
using Gnosis.AI.Perception;

namespace Gnosis.AI;

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

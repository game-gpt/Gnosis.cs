using Gnosis.AI.BehaviorTree;
using Gnosis.AI.Navigation;

namespace Gnosis.AI;

public interface IAISystem
{
    INavigationSystem Navigation { get; }
    IBehaviorTree CreateBehaviorTree(string name);
    IAIController CreateController();
    void DestroyController(IAIController controller);
    void Update(float delta);
}

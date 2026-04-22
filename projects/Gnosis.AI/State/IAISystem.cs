using Gnosis.AI.Behavior;
using Gnosis.Navigation.NavMesh;

namespace Gnosis.AI.State;

public interface IAISystem
{
    INavigationSystem Navigation { get; }
    IBehaviorTree CreateBehaviorTree(string name);
    IAIController CreateController();
    void DestroyController(IAIController controller);
    void Update(float delta);
}

using Gnosis.AI.Behavior;
using Gnosis.Navigation.NavMesh;

namespace Gnosis.AI.State;

public class StubAISystem : IAISystem
{
    public INavigationSystem Navigation => throw new NotImplementedException("AI 系统尚未实现");
    public IBehaviorTree CreateBehaviorTree(string name) { throw new NotImplementedException("AI 系统尚未实现"); }
    public IAIController CreateController() { throw new NotImplementedException("AI 系统尚未实现"); }
    public void DestroyController(IAIController controller) { throw new NotImplementedException("AI 系统尚未实现"); }
    public void Update(float delta) { throw new NotImplementedException("AI 系统尚未实现"); }
}

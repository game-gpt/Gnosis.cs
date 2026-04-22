using Gnosis.AI.Behavior;
using Gnosis.AI.Perception;

namespace Gnosis.AI.State;

public class StubAIController : IAIController
{
    public IBehaviorTree? BehaviorTree => throw new NotImplementedException("AI 系统尚未实现");
    public IAIPerception? Perception => throw new NotImplementedException("AI 系统尚未实现");
    public ITargetSelector TargetSelector => throw new NotImplementedException("AI 系统尚未实现");
    public void Reset() { throw new NotImplementedException("AI 系统尚未实现"); }
    public void SetBehaviorTree(IBehaviorTree tree) { throw new NotImplementedException("AI 系统尚未实现"); }
    public void SetPerception(IAIPerception perception) { throw new NotImplementedException("AI 系统尚未实现"); }
    public void Update(float delta) { throw new NotImplementedException("AI 系统尚未实现"); }
}

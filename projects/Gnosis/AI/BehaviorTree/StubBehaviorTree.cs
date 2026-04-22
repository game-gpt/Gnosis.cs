namespace Gnosis.AI.BehaviorTree;

public class StubBehaviorTree : IBehaviorTree
{
    public string Name => throw new NotImplementedException("AI 系统尚未实现");
    public IBTNode Root => throw new NotImplementedException("AI 系统尚未实现");
    public IBlackboard Blackboard => throw new NotImplementedException("AI 系统尚未实现");
    public BTNodeStatus Status => throw new NotImplementedException("AI 系统尚未实现");
    public bool IsRunning => throw new NotImplementedException("AI 系统尚未实现");
    public void Restart() { throw new NotImplementedException("AI 系统尚未实现"); }
    public void Start() { throw new NotImplementedException("AI 系统尚未实现"); }
    public void Stop() { throw new NotImplementedException("AI 系统尚未实现"); }
    public BTNodeStatus Tick() { throw new NotImplementedException("AI 系统尚未实现"); }
}

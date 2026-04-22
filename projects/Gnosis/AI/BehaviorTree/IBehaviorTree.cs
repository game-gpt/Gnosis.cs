namespace Gnosis.AI.BehaviorTree;

public interface IBehaviorTree
{
    string Name { get; }
    IBTNode Root { get; }
    IBlackboard Blackboard { get; }
    BTNodeStatus Status { get; }
    bool IsRunning { get; }
    void Start();
    void Stop();
    void Restart();
    BTNodeStatus Tick();
}

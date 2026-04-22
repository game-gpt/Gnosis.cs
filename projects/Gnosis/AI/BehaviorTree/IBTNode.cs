namespace Gnosis.AI.BehaviorTree;

public interface IBTNode
{
    string Name { get; }
    BTNodeType NodeType { get; }
    BTNodeStatus Status { get; }
    BTNodeStatus Execute();
    void Reset();
}

public enum BTNodeType
{
    Sequence = 0,
    Selector = 1,
    Parallel = 2,
    Decorator = 3,
    Task = 4,
    Condition = 5
}

using Gnosis.ECS.Component;

namespace Gnosis.AI.State;

public struct AIControllerComponent : IComponent
{
    public IAIController? Controller { get; set; }
    public string? BehaviorTreePath { get; set; }
}

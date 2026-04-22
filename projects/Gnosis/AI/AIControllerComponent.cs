using Gnosis.ECS.Core;

namespace Gnosis.AI;

public struct AIControllerComponent : IComponent
{
    public IAIController? Controller { get; set; }
    public string? BehaviorTreePath { get; set; }
}

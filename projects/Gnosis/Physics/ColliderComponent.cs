using Gnosis.Core;

namespace Gnosis.Physics;

public struct ColliderComponent : IComponent
{
    public ICollider? Collider { get; set; }
    public bool IsTrigger { get; set; }
    public string? MaterialPath { get; set; }
}

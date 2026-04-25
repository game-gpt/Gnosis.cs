using Gnosis.ECS.Component;
using Gnosis.Physics.Dynamics;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics;

public struct RigidBodyComponent : IComponent
{
    public IRigidBody? RigidBody { get; set; }
    public RigidBodyType BodyType { get; set; }
    public float Mass { get; set; }
    public bool UseGravity { get; set; }
}

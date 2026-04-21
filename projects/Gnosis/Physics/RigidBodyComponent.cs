using Gnosis.Core;
using Gnosis.Physics.Interface;

namespace Gnosis.Physics;

public struct RigidBodyComponent : IComponent
{
    public IRigidBody? RigidBody { get; set; }
    public RigidBodyType BodyType { get; set; }
    public float Mass { get; set; }
    public bool UseGravity { get; set; }
}

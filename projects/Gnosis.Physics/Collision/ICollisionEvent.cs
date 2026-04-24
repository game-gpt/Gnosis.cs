using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Collision;

public interface ICollisionEvent
{
    ICollider ThisCollider { get; }
    ICollider OtherCollider { get; }
    Vector3 ContactPoint { get; }
    Vector3 ContactNormal { get; }
    float PenetrationDepth { get; }
    float RelativeVelocity { get; }
}

public interface ITriggerEvent
{
    ICollider ThisCollider { get; }
    ICollider OtherCollider { get; }
}

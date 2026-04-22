using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Collision;

public interface ICollisionEvent
{
    ICollider ThisCollider { get; }
    ICollider OtherCollider { get; }
    float[] ContactPoint { get; }
    float[] ContactNormal { get; }
    float PenetrationDepth { get; }
    float RelativeVelocity { get; }
}

public interface ITriggerEvent
{
    ICollider ThisCollider { get; }
    ICollider OtherCollider { get; }
}

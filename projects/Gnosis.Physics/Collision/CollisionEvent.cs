using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Collision;

public sealed class CollisionEvent : ICollisionEvent
{
    #region 属性

    public ICollider ThisCollider { get; }

    public ICollider OtherCollider { get; }

    public float[] ContactPoint { get; }

    public float[] ContactNormal { get; }

    public float PenetrationDepth { get; }

    public float RelativeVelocity { get; }

    #endregion

    #region 构造函数

    public CollisionEvent(
        ICollider thisCollider,
        ICollider otherCollider,
        float[] contactPoint,
        float[] contactNormal,
        float penetrationDepth,
        float relativeVelocity)
    {
        ThisCollider = thisCollider;
        OtherCollider = otherCollider;
        ContactPoint = contactPoint;
        ContactNormal = contactNormal;
        PenetrationDepth = penetrationDepth;
        RelativeVelocity = relativeVelocity;
    }

    #endregion
}

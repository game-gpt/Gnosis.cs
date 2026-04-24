using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Collision;

public sealed class CollisionEvent : ICollisionEvent
{
    #region 属性

    public ICollider ThisCollider { get; }

    public ICollider OtherCollider { get; }

    public Vector3 ContactPoint { get; }

    public Vector3 ContactNormal { get; }

    public float PenetrationDepth { get; }

    public float RelativeVelocity { get; }

    #endregion

    #region 构造函数

    public CollisionEvent(
        ICollider thisCollider,
        ICollider otherCollider,
        Vector3 contactPoint,
        Vector3 contactNormal,
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

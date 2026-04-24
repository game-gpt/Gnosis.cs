using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Query;

public sealed class RaycastResult : IRaycastResult
{
    #region 属性

    public bool HasHit { get; }

    public ICollider? Collider { get; }

    public Vector3 Point { get; }

    public Vector3 Normal { get; }

    public float Distance { get; }

    #endregion

    #region 构造函数

    public RaycastResult()
    {
        HasHit = false;
        Point = Vector3.Zero;
        Normal = Vector3.UnitY;
        Distance = 0f;
    }

    public RaycastResult(ICollider collider, Vector3 point, Vector3 normal, float distance)
    {
        HasHit = true;
        Collider = collider;
        Point = point;
        Normal = normal;
        Distance = distance;
    }

    #endregion
}

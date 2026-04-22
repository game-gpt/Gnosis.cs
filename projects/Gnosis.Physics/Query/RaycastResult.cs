using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Query;

public sealed class RaycastResult : IRaycastResult
{
    #region 属性

    public bool HasHit { get; }

    public ICollider? Collider { get; }

    public float[] Point { get; }

    public float[] Normal { get; }

    public float Distance { get; }

    #endregion

    #region 构造函数

    public RaycastResult()
    {
        HasHit = false;
        Point = [0f, 0f, 0f];
        Normal = [0f, 1f, 0f];
        Distance = 0f;
    }

    public RaycastResult(ICollider collider, float[] point, float[] normal, float distance)
    {
        HasHit = true;
        Collider = collider;
        Point = point;
        Normal = normal;
        Distance = distance;
    }

    #endregion
}

using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Query;

public sealed class OverlapResult : IOverlapResult
{
    #region 字段

    private readonly List<ICollider> _colliders = new();

    #endregion

    #region 属性

    public IReadOnlyList<ICollider> Colliders => _colliders;

    public int Count => _colliders.Count;

    #endregion

    #region 内部方法

    internal void AddCollider(ICollider collider)
    {
        _colliders.Add(collider);
    }

    internal void Clear()
    {
        _colliders.Clear();
    }

    #endregion
}

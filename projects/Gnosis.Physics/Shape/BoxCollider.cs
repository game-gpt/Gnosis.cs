using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Physics.Shape;

public sealed class BoxCollider : IBoxCollider
{
    #region 属性

    public string Name { get; }

    public bool IsTrigger { get; set; }

    public IPhysicsMaterial? Material { get; set; }

    public Vector3 Center { get; set; } = Vector3.Zero;

    public Vector3 Size => new(HalfExtentsX * 2f, HalfExtentsY * 2f, HalfExtentsZ * 2f);

    public float HalfExtentsX { get; set; } = 0.5f;

    public float HalfExtentsY { get; set; } = 0.5f;

    public float HalfExtentsZ { get; set; } = 0.5f;

    #endregion

    #region 构造函数

    public BoxCollider(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    #endregion
}

using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Physics.Shape;

public sealed class SphereCollider : ISphereCollider
{
    #region 属性

    public string Name { get; }

    public bool IsTrigger { get; set; }

    public IPhysicsMaterial? Material { get; set; }

    public Vector3 Center { get; set; } = Vector3.Zero;

    public Vector3 Size => new(Radius * 2f, Radius * 2f, Radius * 2f);

    public float Radius { get; set; } = 0.5f;

    #endregion

    #region 构造函数

    public SphereCollider(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    #endregion
}

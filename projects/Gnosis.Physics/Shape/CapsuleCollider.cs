using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Physics.Shape;

public sealed class CapsuleCollider : ICapsuleCollider
{
    #region 属性

    public string Name { get; }

    public bool IsTrigger { get; set; }

    public IPhysicsMaterial? Material { get; set; }

    public Vector3 Center { get; set; } = Vector3.Zero;

    public Vector3 Size => new(Radius * 2f, Height, Radius * 2f);

    public float Radius { get; set; } = 0.25f;

    public float Height { get; set; } = 1.0f;

    public int Direction { get; set; } = 1;

    #endregion

    #region 构造函数

    public CapsuleCollider(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    #endregion
}

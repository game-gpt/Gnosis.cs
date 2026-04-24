using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Physics.Shape;

public sealed class MeshCollider : IMeshCollider
{
    #region 属性

    public string Name { get; }

    public bool IsTrigger { get; set; }

    public IPhysicsMaterial? Material { get; set; }

    public Vector3 Center { get; set; } = Vector3.Zero;

    public Vector3 Size { get; set; } = Vector3.One;

    public string MeshPath { get; set; } = "";

    public bool IsConvex { get; set; }

    public bool IsCooked { get; private set; }

    #endregion

    #region 构造函数

    public MeshCollider(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    #endregion

    #region IMeshCollider 实现

    public void CookMesh()
    {
        IsCooked = true;
    }

    #endregion
}

namespace Gnosis.Physics.Shape;

public sealed class BoxCollider : IBoxCollider
{
    #region 属性

    public string Name { get; }

    public bool IsTrigger { get; set; }

    public IPhysicsMaterial? Material { get; set; }

    public float[] Center { get; set; } = [0f, 0f, 0f];

    public float[] Size => [HalfExtentsX * 2f, HalfExtentsY * 2f, HalfExtentsZ * 2f];

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

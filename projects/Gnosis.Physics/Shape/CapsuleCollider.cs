namespace Gnosis.Physics.Shape;

public sealed class CapsuleCollider : ICapsuleCollider
{
    #region 属性

    public string Name { get; }

    public bool IsTrigger { get; set; }

    public IPhysicsMaterial? Material { get; set; }

    public float[] Center { get; set; } = [0f, 0f, 0f];

    public float[] Size => [Radius * 2f, Height, Radius * 2f];

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

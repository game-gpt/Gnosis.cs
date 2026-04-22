namespace Gnosis.Physics.Shape;

public sealed class SphereCollider : ISphereCollider
{
    #region 属性

    public string Name { get; }

    public bool IsTrigger { get; set; }

    public IPhysicsMaterial? Material { get; set; }

    public float[] Center { get; set; } = [0f, 0f, 0f];

    public float[] Size => [Radius * 2f, Radius * 2f, Radius * 2f];

    public float Radius { get; set; } = 0.5f;

    #endregion

    #region 构造函数

    public SphereCollider(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    #endregion
}

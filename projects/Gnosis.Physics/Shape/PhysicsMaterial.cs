namespace Gnosis.Physics.Shape;

public sealed class PhysicsMaterial : IPhysicsMaterial
{
    #region 属性

    public string Name { get; }

    public float Friction { get; set; } = 0.5f;

    public float Restitution { get; set; } = 0.0f;

    public float FrictionCombine { get; set; } = 0.5f;

    public float RestitutionCombine { get; set; } = 0.0f;

    #endregion

    #region 构造函数

    public PhysicsMaterial(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    #endregion
}

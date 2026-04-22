namespace Gnosis.Animation.IK;

public sealed class IKConstraint : IIKConstraint
{
    #region 属性

    public string Name { get; }

    public IKConstraintType Type { get; }

    public float[] TargetPosition { get; set; } = [0f, 0f, 0f];

    public float[] TargetRotation { get; set; } = [0f, 0f, 0f, 1f];

    public int ChainLength { get; set; } = 2;

    public float Weight { get; set; } = 1.0f;

    #endregion

    #region 构造函数

    public IKConstraint(string name, IKConstraintType type)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
    }

    #endregion
}

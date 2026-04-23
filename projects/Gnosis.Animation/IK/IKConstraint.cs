using Gnosis.Core.Math;

namespace Gnosis.Animation.IK;

public sealed class IKConstraint : IIKConstraint
{
    #region 属性

    public string Name { get; }

    public IKConstraintType Type { get; }

    public Vector3 TargetPosition { get; set; } = new(0f, 0f, 0f);

    public Quaternion TargetRotation { get; set; } = Quaternion.Identity;

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

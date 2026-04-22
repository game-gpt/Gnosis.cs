namespace Gnosis.Animation.IK;

public sealed class IKSolver : IIKSolver
{
    #region 属性

    public string Name { get; }

    public IKConstraintType Type { get; }

    public bool IsActive { get; set; } = true;

    #endregion

    #region 构造函数

    public IKSolver(string name, IKConstraintType type)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
    }

    #endregion

    #region IIKSolver 实现

    public void Solve(ISkeleton skeleton, IIKConstraint? constraint)
    {
    }

    #endregion
}

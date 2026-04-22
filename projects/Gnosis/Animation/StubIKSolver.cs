namespace Gnosis.Animation;

public class StubIKSolver : IIKSolver
{
    public string Name => throw new NotImplementedException("动画系统尚未实现");
    public bool IsActive { get => throw new NotImplementedException("动画系统尚未实现"); set => throw new NotImplementedException("动画系统尚未实现"); }

    public void Solve(ISkeleton skeleton, IIKConstraint constraint)
    {
        throw new NotImplementedException("动画系统尚未实现");
    }
}

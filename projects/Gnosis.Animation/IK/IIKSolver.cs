using Gnosis.Core.Math;

namespace Gnosis.Animation.IK;

public interface IIKSolver
{
    string Name { get; }
    bool IsActive { get; set; }
    void Solve(ISkeleton skeleton, IIKConstraint constraint);
}

public interface IIKConstraint
{
    string Name { get; }
    IKConstraintType Type { get; }
    Vector3 TargetPosition { get; set; }
    Quaternion TargetRotation { get; set; }
    int ChainLength { get; set; }
    float Weight { get; set; }
}

public enum IKConstraintType
{
    Position = 0,
    Rotation = 1,
    Aim = 2,
    TwoBone = 3,
    FABRIK = 4
}

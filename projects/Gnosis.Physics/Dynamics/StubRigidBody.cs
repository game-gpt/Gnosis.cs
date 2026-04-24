using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Physics.Dynamics;

public class StubRigidBody : IRigidBody
{
    public string Name => throw new NotImplementedException("物理系统尚未实现");
    public RigidBodyType BodyType { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public float Mass { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public float Drag { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public float AngularDrag { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public bool UseGravity { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public bool IsKinematic { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public Vector3 Velocity { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public Vector3 AngularVelocity { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public Vector3 Position { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public Quaternion Rotation { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }

    public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public void AddForceAtPosition(Vector3 force, Vector3 position, ForceMode mode = ForceMode.Force)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public void AddTorque(Vector3 torque, ForceMode mode = ForceMode.Force)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }
}

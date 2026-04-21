namespace Gnosis.Physics;

public class StubRigidBody : IRigidBody
{
    public string Name => throw new NotImplementedException("物理系统尚未实现");
    public RigidBodyType BodyType { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public float Mass { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public float Drag { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public float AngularDrag { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public bool UseGravity { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public bool IsKinematic { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public float[] Velocity { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public float[] AngularVelocity { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public float[] Position { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public float[] Rotation { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }

    public void AddForce(float[] force, ForceMode mode = ForceMode.Force)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public void AddForceAtPosition(float[] force, float[] position, ForceMode mode = ForceMode.Force)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public void AddTorque(float[] torque, ForceMode mode = ForceMode.Force)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }
}

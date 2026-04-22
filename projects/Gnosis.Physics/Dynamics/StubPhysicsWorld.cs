using Gnosis.Physics.Query;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Dynamics;

public class StubPhysicsWorld : IPhysicsWorld
{
    public float FixedDeltaTime { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public float[] Gravity { get => throw new NotImplementedException("物理系统尚未实现"); set => throw new NotImplementedException("物理系统尚未实现"); }
    public int BodyCount => throw new NotImplementedException("物理系统尚未实现");

    public void AttachCollider(IRigidBody body, ICollider collider)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public IBoxCollider CreateBoxCollider(string name)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public ICapsuleCollider CreateCapsuleCollider(string name)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public IMeshCollider CreateMeshCollider(string name)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public IRigidBody CreateRigidBody(string name, RigidBodyType type)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public void DestroyRigidBody(IRigidBody body)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public void DetachCollider(IRigidBody body, ICollider collider)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public IOverlapResult OverlapBox(float[] center, float[] halfExtents)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public IOverlapResult OverlapSphere(float[] center, float radius)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public IRaycastResult Raycast(float[] origin, float[] direction, float maxDistance)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public IRaycastResult[] RaycastAll(float[] origin, float[] direction, float maxDistance)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public ISphereCollider CreateSphereCollider(string name)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public void Step(float delta)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public void SyncTransforms()
    {
        throw new NotImplementedException("物理系统尚未实现");
    }
}

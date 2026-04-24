using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Physics.Query;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Dynamics;

public sealed class PhysicsWorld : IPhysicsWorld
{
    #region 字段

    private readonly List<IRigidBody> _bodies = new();
    private readonly Dictionary<string, IRigidBody> _bodiesByName = new();

    #endregion

    #region 属性

    public float FixedDeltaTime { get; set; } = 1f / 60f;

    public Vector3 Gravity { get; set; } = new(0f, -9.81f, 0f);

    public int BodyCount => _bodies.Count;

    #endregion

    #region IPhysicsWorld 实现

    public IRigidBody CreateRigidBody(string name, RigidBodyType type)
    {
        var body = new RigidBody(name, type);
        _bodies.Add(body);
        _bodiesByName[name] = body;
        return body;
    }

    public void DestroyRigidBody(IRigidBody body)
    {
        _bodies.Remove(body);

        if (body is RigidBody rb)
        {
            _bodiesByName.Remove(rb.Name);
        }
    }

    public IBoxCollider CreateBoxCollider(string name)
    {
        return new BoxCollider(name);
    }

    public ISphereCollider CreateSphereCollider(string name)
    {
        return new SphereCollider(name);
    }

    public ICapsuleCollider CreateCapsuleCollider(string name)
    {
        return new CapsuleCollider(name);
    }

    public IMeshCollider CreateMeshCollider(string name)
    {
        return new MeshCollider(name);
    }

    public void AttachCollider(IRigidBody body, ICollider collider)
    {
        if (body is RigidBody rb)
        {
            rb.AddCollider(collider);
        }
    }

    public void DetachCollider(IRigidBody body, ICollider collider)
    {
        if (body is RigidBody rb)
        {
            rb.RemoveCollider(collider);
        }
    }

    public IRaycastResult Raycast(Vector3 origin, Vector3 direction, float maxDistance)
    {
        return new RaycastResult();
    }

    public IRaycastResult[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance)
    {
        return [];
    }

    public IOverlapResult OverlapSphere(Vector3 center, float radius)
    {
        return new OverlapResult();
    }

    public IOverlapResult OverlapBox(Vector3 center, Vector3 halfExtents)
    {
        return new OverlapResult();
    }

    public void Step(float delta)
    {
        foreach (var body in _bodies)
        {
            if (body is RigidBody rb)
            {
                rb.Integrate(delta, Gravity);
            }
        }
    }

    public void SyncTransforms()
    {
    }

    #endregion
}

using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Physics.Query;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Dynamics;

public interface IPhysicsWorld
{
    float FixedDeltaTime { get; set; }
    Vector3 Gravity { get; set; }
    int BodyCount { get; }
    IRigidBody CreateRigidBody(string name, RigidBodyType type);
    void DestroyRigidBody(IRigidBody body);
    IBoxCollider CreateBoxCollider(string name);
    ISphereCollider CreateSphereCollider(string name);
    ICapsuleCollider CreateCapsuleCollider(string name);
    IMeshCollider CreateMeshCollider(string name);
    void AttachCollider(IRigidBody body, ICollider collider);
    void DetachCollider(IRigidBody body, ICollider collider);
    IRaycastResult Raycast(Vector3 origin, Vector3 direction, float maxDistance);
    IRaycastResult[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance);
    IOverlapResult OverlapSphere(Vector3 center, float radius);
    IOverlapResult OverlapBox(Vector3 center, Vector3 halfExtents);
    void Step(float delta);
    void SyncTransforms();
}

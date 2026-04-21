namespace Gnosis.Physics;

public interface IPhysicsWorld
{
    float FixedDeltaTime { get; set; }
    float[] Gravity { get; set; }
    int BodyCount { get; }
    IRigidBody CreateRigidBody(string name, RigidBodyType type);
    void DestroyRigidBody(IRigidBody body);
    IBoxCollider CreateBoxCollider(string name);
    ISphereCollider CreateSphereCollider(string name);
    ICapsuleCollider CreateCapsuleCollider(string name);
    IMeshCollider CreateMeshCollider(string name);
    void AttachCollider(IRigidBody body, ICollider collider);
    void DetachCollider(IRigidBody body, ICollider collider);
    IRaycastResult Raycast(float[] origin, float[] direction, float maxDistance);
    IRaycastResult[] RaycastAll(float[] origin, float[] direction, float maxDistance);
    IOverlapResult OverlapSphere(float[] center, float radius);
    IOverlapResult OverlapBox(float[] center, float[] halfExtents);
    void Step(float delta);
    void SyncTransforms();
}

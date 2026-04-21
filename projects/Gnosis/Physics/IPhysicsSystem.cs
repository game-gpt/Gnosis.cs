namespace Gnosis.Physics;

public interface IPhysicsSystem
{
    IPhysicsWorld CreateWorld();
    void DestroyWorld(IPhysicsWorld world);
    IPhysicsWorld DefaultWorld { get; }
    void Update(float delta);
}

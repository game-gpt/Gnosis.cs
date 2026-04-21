using Gnosis.Physics.Interface;

namespace Gnosis.Physics.Implementation;

public class StubPhysicsSystem : IPhysicsSystem
{
    public IPhysicsWorld DefaultWorld => throw new NotImplementedException("物理系统尚未实现");

    public IPhysicsWorld CreateWorld()
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public void DestroyWorld(IPhysicsWorld world)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }

    public void Update(float delta)
    {
        throw new NotImplementedException("物理系统尚未实现");
    }
}

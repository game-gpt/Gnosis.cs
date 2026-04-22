namespace Gnosis.Physics.Dynamics;

public sealed class PhysicsSystem : IPhysicsSystem
{
    #region 字段

    private readonly List<IPhysicsWorld> _worlds = new();

    #endregion

    #region 属性

    public IPhysicsWorld DefaultWorld { get; }

    #endregion

    #region 构造函数

    public PhysicsSystem()
    {
        DefaultWorld = new PhysicsWorld();
        _worlds.Add(DefaultWorld);
    }

    #endregion

    #region IPhysicsSystem 实现

    public IPhysicsWorld CreateWorld()
    {
        var world = new PhysicsWorld();
        _worlds.Add(world);
        return world;
    }

    public void DestroyWorld(IPhysicsWorld world)
    {
        _worlds.Remove(world);
    }

    public void Update(float delta)
    {
        foreach (var world in _worlds)
        {
            if (world is PhysicsWorld pw)
            {
                pw.Step(delta);
            }
        }
    }

    #endregion
}

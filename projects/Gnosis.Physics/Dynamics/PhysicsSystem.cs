using Gnosis.Physics.BroadPhase;
using Gnosis.Physics.NarrowPhase;
using Gnosis.Physics.Solver;

namespace Gnosis.Physics.Dynamics;

public sealed class PhysicsSystem : IPhysicsSystem
{
    #region 字段

    private readonly List<IPhysicsWorld> _worlds = new();
    private readonly SimdIntegrationBatch _simdBatch = new();
    private bool _enableSimdIntegration = true;
    private bool _enableParallelWorlds = true;

    #endregion

    #region 属性

    public IPhysicsWorld DefaultWorld { get; }

    public bool EnableSimdIntegration
    {
        get => _enableSimdIntegration;
        set => _enableSimdIntegration = value;
    }

    public bool EnableParallelWorlds
    {
        get => _enableParallelWorlds;
        set => _enableParallelWorlds = value;
    }

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

    public IPhysicsWorld CreateWorld(float cellSize)
    {
        var world = new PhysicsWorld(cellSize);
        _worlds.Add(world);
        return world;
    }

    public void DestroyWorld(IPhysicsWorld world)
    {
        _worlds.Remove(world);
    }

    public void Update(float delta)
    {
        if (_enableParallelWorlds && _worlds.Count > 1)
        {
            Parallel.ForEach(_worlds, world => StepWorld(world, delta));
        }
        else
        {
            foreach (var world in _worlds)
            {
                StepWorld(world, delta);
            }
        }
    }

    #endregion

    #region 私有方法

    private void StepWorld(IPhysicsWorld world, float delta)
    {
        if (world is not PhysicsWorld pw)
        {
            world.Step(delta);
            return;
        }

        if (_enableSimdIntegration)
        {
            pw.StepSimd(delta, _simdBatch);
        }
        else
        {
            pw.Step(delta);
        }
    }

    #endregion
}

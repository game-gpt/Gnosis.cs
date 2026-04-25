using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Gnosis.ECS.Archetype;
using Gnosis.ECS.Entity;
using Gnosis.ECS.Query;
using Gnosis.ECS.System;
using Gnosis.ECS.World;

namespace Gnosis.Benchmarks;

[Config(typeof(ArchetypeTraversalConfig))]
[MemoryDiagnoser]
[RankColumn]
public class ArchetypeTraversalBenchmarks
{
    private class ArchetypeTraversalConfig : ManualConfig
    {
        public ArchetypeTraversalConfig()
        {
            AddJob(Job.ShortRun
                .WithWarmupCount(3)
                .WithIterationCount(10));
            AddColumn(StatisticColumn.P95);
        }
    }

    private struct Position : IComponent
    {
        public float X;
        public float Y;
        public float Z;
    }

    private struct Velocity : IComponent
    {
        public float X;
        public float Y;
        public float Z;
    }

    private struct Health : IComponent
    {
        public float Value;
    }

    private World _world = null!;
    private EntityId[] _entities = null!;
    private QueryIterator _queryIterator = null!;
    private MovementInlineSystem _inlineSystem = null!;

    [Params(100_000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _world = new World();
        _entities = new EntityId[EntityCount];

        for (var i = 0; i < EntityCount; i++)
        {
            var entity = _world.CreateEntity();
            _world.AddComponent(entity, new Position { X = i, Y = i * 0.5f, Z = 0 });
            _world.AddComponent(entity, new Velocity { X = 1, Y = 0.5f, Z = 0 });
            _entities[i] = entity;
        }

        var query = (EntityQuery)_world.CreateQuery();
        query.All<Position>();
        _queryIterator = query.Iterate();

        _inlineSystem = new MovementInlineSystem();
        _inlineSystem.SetWorld(_world);
        _inlineSystem.Initialize();
    }

    #region 单组件遍历

    [Benchmark(Baseline = true)]
    public void ForEach_Position()
    {
        _queryIterator.ForEach<Position>(static (EntityId entity, ref Position pos) =>
        {
            pos.X += 1.0f;
        });
    }

    [Benchmark]
    public void EntitiesWithRef_Position()
    {
        _queryIterator.EntitiesWithRef<Position>(static (EntityId entity, ref Position pos) =>
        {
            pos.X += 1.0f;
        });
    }

    [Benchmark]
    public void InlineSystem_Position()
    {
        _inlineSystem.Update(0.016f);
    }

    #endregion

    #region 双组件遍历

    [Benchmark]
    public void ForEach_PositionVelocity()
    {
        var query = (EntityQuery)_world.CreateQuery();
        query.All<Position>();
        query.All<Velocity>();
        var iterator = query.Iterate();

        iterator.ForEach<Position, Velocity>(static (EntityId entity, ref Position pos, ref Velocity vel) =>
        {
            pos.X += vel.X * 0.016f;
            pos.Y += vel.Y * 0.016f;
            pos.Z += vel.Z * 0.016f;
        });
    }

    [Benchmark]
    public void EntitiesWithRef_PositionVelocity()
    {
        var query = (EntityQuery)_world.CreateQuery();
        query.All<Position>();
        query.All<Velocity>();
        var iterator = query.Iterate();

        iterator.EntitiesWithRef<Position, Velocity>(static (EntityId entity, ref Position pos, ref Velocity vel) =>
        {
            pos.X += vel.X * 0.016f;
            pos.Y += vel.Y * 0.016f;
            pos.Z += vel.Z * 0.016f;
        });
    }

    #endregion

    #region 直接 Chunk 遍历（最低开销基准）

    [Benchmark]
    public void DirectChunkTraversal_Position()
    {
        foreach (var archetype in _world.Archetypes.GetArchetypes())
        {
            var slot = archetype.GetComponentSlot<Position>();
            if (slot < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var compArray = chunk.GetComponentArrayBySlot<Position>(slot);
                var count = chunk.Count;

                for (var i = 0; i < count; i++)
                {
                    compArray[i].X += 1.0f;
                }
            }
        }
    }

    #endregion

    #region 内联系统实现

    private sealed class MovementInlineSystem : InlineSystem<Position>
    {
        public override SystemPhase Phase => SystemPhase.Update;

        protected override void Execute(EntityId entity, ref Position pos, float delta)
        {
            pos.X += 1.0f;
        }
    }

    #endregion
}

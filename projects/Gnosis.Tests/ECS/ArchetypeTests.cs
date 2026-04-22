using Gnosis.ECS.Archetype;
using Gnosis.ECS.Component;
using Gnosis.ECS.Entity;
using NUnit.Framework;

namespace Gnosis.ECS;

[TestFixture]
public class ChunkTests
{
    private struct TestPosition
    {
        public float X;
        public float Y;
        public float Z;

        public TestPosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    private struct TestVelocity
    {
        public float Vx;
        public float Vy;

        public TestVelocity(float vx, float vy)
        {
            Vx = vx;
            Vy = vy;
        }
    }

    [Test]
    public void AddEntity_ReturnsIncrementalIndex()
    {
        var chunk = new Chunk([typeof(TestPosition), typeof(TestVelocity)], capacity: 4);

        var idx0 = chunk.AddEntity(new EntityId(1, 0));
        var idx1 = chunk.AddEntity(new EntityId(2, 0));
        var idx2 = chunk.AddEntity(new EntityId(3, 0));

        Assert.That(idx0, Is.EqualTo(0), "第一个实体索引应为 0");
        Assert.That(idx1, Is.EqualTo(1), "第二个实体索引应为 1");
        Assert.That(idx2, Is.EqualTo(2), "第三个实体索引应为 2");
        Assert.That(chunk.Count, Is.EqualTo(3), "Count 应为 3");
    }

    [Test]
    public void AddEntity_FullChunk_ReturnsMinusOne()
    {
        var chunk = new Chunk([typeof(TestPosition)], capacity: 2);

        chunk.AddEntity(new EntityId(1, 0));
        chunk.AddEntity(new EntityId(2, 0));
        var result = chunk.AddEntity(new EntityId(3, 0));

        Assert.That(result, Is.EqualTo(-1), "满 Chunk 应返回 -1");
        Assert.That(chunk.IsFull, Is.True, "IsFull 应为 true");
    }

    [Test]
    public void RemoveEntity_SwapBackMaintainsContiguity()
    {
        var chunk = new Chunk([typeof(TestPosition)], capacity: 4);

        var e1 = new EntityId(1, 0);
        var e2 = new EntityId(2, 0);
        var e3 = new EntityId(3, 0);

        chunk.AddEntity(e1);
        chunk.AddEntity(e2);
        chunk.AddEntity(e3);

        chunk.SetComponent(0, new TestPosition(1, 0, 0));
        chunk.SetComponent(1, new TestPosition(0, 2, 0));
        chunk.SetComponent(2, new TestPosition(0, 0, 3));

        chunk.RemoveEntity(0);

        Assert.That(chunk.Count, Is.EqualTo(2), "移除后 Count 应为 2");
        Assert.That(chunk.GetEntity(0), Is.EqualTo(e3), "swap-back 后索引 0 应为 e3");
        var pos = chunk.GetComponent<TestPosition>(0);
        Assert.That(pos.Z, Is.EqualTo(3f), "e3 的组件数据应被正确交换");
    }

    [Test]
    public void GetComponent_SetComponent_RoundTrip()
    {
        var chunk = new Chunk([typeof(TestPosition)], capacity: 4);
        chunk.AddEntity(new EntityId(1, 0));

        chunk.SetComponent(0, new TestPosition(5, 10, 15));
        ref var pos = ref chunk.GetComponent<TestPosition>(0);

        Assert.That(pos.X, Is.EqualTo(5f), "X 应为 5");
        Assert.That(pos.Y, Is.EqualTo(10f), "Y 应为 10");
        Assert.That(pos.Z, Is.EqualTo(15f), "Z 应为 15");
    }

    [Test]
    public void GetComponent_RefModification()
    {
        var chunk = new Chunk([typeof(TestPosition)], capacity: 4);
        chunk.AddEntity(new EntityId(1, 0));
        chunk.SetComponent(0, new TestPosition(1, 2, 3));

        ref var pos = ref chunk.GetComponent<TestPosition>(0);
        pos.X = 100;

        var result = chunk.GetComponent<TestPosition>(0);
        Assert.That(result.X, Is.EqualTo(100f), "通过引用修改后 X 应为 100");
    }

    [Test]
    public void HasComponent_ReturnsCorrectResult()
    {
        var chunk = new Chunk([typeof(TestPosition), typeof(TestVelocity)], capacity: 4);

        Assert.That(chunk.HasComponent<TestPosition>(), Is.True, "应包含 TestPosition");
        Assert.That(chunk.HasComponent<TestVelocity>(), Is.True, "应包含 TestVelocity");
        Assert.That(chunk.HasComponent<int>(), Is.False, "不应包含 int");
    }

    [Test]
    public void Clear_ResetsCount()
    {
        var chunk = new Chunk([typeof(TestPosition)], capacity: 4);
        chunk.AddEntity(new EntityId(1, 0));
        chunk.AddEntity(new EntityId(2, 0));

        chunk.Clear();

        Assert.That(chunk.Count, Is.EqualTo(0), "Clear 后 Count 应为 0");
        Assert.That(chunk.IsFull, Is.False, "Clear 后 IsFull 应为 false");
    }

    [Test]
    public void CopyEntityTo_CopiesSharedComponents()
    {
        var sourceChunk = new Chunk([typeof(TestPosition), typeof(TestVelocity)], capacity: 4);
        var targetChunk = new Chunk([typeof(TestPosition), typeof(TestVelocity)], capacity: 4);

        sourceChunk.AddEntity(new EntityId(1, 0));
        sourceChunk.SetComponent(0, new TestPosition(1, 2, 3));
        sourceChunk.SetComponent(0, new TestVelocity(4, 5));

        targetChunk.AddEntity(new EntityId(1, 0));

        sourceChunk.CopyEntityTo(0, targetChunk, 0);

        var pos = targetChunk.GetComponent<TestPosition>(0);
        var vel = targetChunk.GetComponent<TestVelocity>(0);

        Assert.That(pos.X, Is.EqualTo(1f), "复制后 X 应为 1");
        Assert.That(vel.Vx, Is.EqualTo(4f), "复制后 Vx 应为 4");
    }

    [Test]
    public void GetAllEntities_ReturnsCorrectEntities()
    {
        var chunk = new Chunk([typeof(TestPosition)], capacity: 4);
        var e1 = new EntityId(1, 0);
        var e2 = new EntityId(2, 0);

        chunk.AddEntity(e1);
        chunk.AddEntity(e2);

        var entities = chunk.GetAllEntities();

        Assert.That(entities.Count, Is.EqualTo(2), "应返回 2 个实体");
        Assert.That(entities[0], Is.EqualTo(e1), "第一个应为 e1");
        Assert.That(entities[1], Is.EqualTo(e2), "第二个应为 e2");
    }
}

[TestFixture]
public class ArchetypeTests
{
    private struct TestPosition
    {
        public float X;
        public float Y;
        public float Z;

        public TestPosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    private struct TestVelocity
    {
        public float Vx;
        public float Vy;

        public TestVelocity(float vx, float vy)
        {
            Vx = vx;
            Vy = vy;
        }
    }

    [Test]
    public void AddEntity_IncrementsEntityCount()
    {
        var archetype = new Archetype.Archetype([typeof(TestPosition)]);

        archetype.AddEntity(new EntityId(1, 0));
        archetype.AddEntity(new EntityId(2, 0));

        Assert.That(archetype.EntityCount, Is.EqualTo(2), "EntityCount 应为 2");
    }

    [Test]
    public void RemoveEntity_DecrementsEntityCount()
    {
        var archetype = new Archetype.Archetype([typeof(TestPosition)]);

        var e1 = new EntityId(1, 0);
        archetype.AddEntity(e1);
        archetype.AddEntity(new EntityId(2, 0));

        archetype.RemoveEntity(e1);

        Assert.That(archetype.EntityCount, Is.EqualTo(1), "移除后 EntityCount 应为 1");
    }

    [Test]
    public void RemoveEntity_NonExistent_ReturnsFalse()
    {
        var archetype = new Archetype.Archetype([typeof(TestPosition)]);

        var result = archetype.RemoveEntity(new EntityId(999, 0));

        Assert.That(result, Is.False, "移除不存在的实体应返回 false");
    }

    [Test]
    public void SetComponent_GetComponent_RoundTrip()
    {
        var archetype = new Archetype.Archetype([typeof(TestPosition)]);

        var e1 = new EntityId(1, 0);
        archetype.AddEntity(e1);
        archetype.SetComponent(e1, new TestPosition(10, 20, 30));

        ref var pos = ref archetype.GetComponent<TestPosition>(e1);

        Assert.That(pos.X, Is.EqualTo(10f), "X 应为 10");
        Assert.That(pos.Y, Is.EqualTo(20f), "Y 应为 20");
        Assert.That(pos.Z, Is.EqualTo(30f), "Z 应为 30");
    }

    [Test]
    public void HasComponent_ReturnsCorrectResult()
    {
        var archetype = new Archetype.Archetype([typeof(TestPosition), typeof(TestVelocity)]);

        Assert.That(archetype.HasComponent<TestPosition>(), Is.True, "应包含 TestPosition");
        Assert.That(archetype.HasComponent<TestVelocity>(), Is.True, "应包含 TestVelocity");
        Assert.That(archetype.HasComponent<int>(), Is.False, "不应包含 int");
    }

    [Test]
    public void GetEntities_ReturnsAllEntities()
    {
        var archetype = new Archetype.Archetype([typeof(TestPosition)]);

        var e1 = new EntityId(1, 0);
        var e2 = new EntityId(2, 0);
        var e3 = new EntityId(3, 0);

        archetype.AddEntity(e1);
        archetype.AddEntity(e2);
        archetype.AddEntity(e3);

        var entities = archetype.GetEntities().ToList();

        Assert.That(entities.Count, Is.EqualTo(3), "应返回 3 个实体");
        Assert.That(entities, Does.Contain(e1), "应包含 e1");
        Assert.That(entities, Does.Contain(e2), "应包含 e2");
        Assert.That(entities, Does.Contain(e3), "应包含 e3");
    }

    [Test]
    public void MultipleChunks_WhenCapacityExceeded()
    {
        var archetype = new Archetype.Archetype([typeof(TestPosition)], chunkCapacity: 2);

        archetype.AddEntity(new EntityId(1, 0));
        archetype.AddEntity(new EntityId(2, 0));
        archetype.AddEntity(new EntityId(3, 0));

        Assert.That(archetype.EntityCount, Is.EqualTo(3), "EntityCount 应为 3");
        Assert.That(archetype.Chunks.Count, Is.EqualTo(2), "应有 2 个 Chunk");
    }

    [Test]
    public void RemoveEntity_SwapBack_UpdatesLocation()
    {
        var archetype = new Archetype.Archetype([typeof(TestPosition)], chunkCapacity: 4);

        var e1 = new EntityId(1, 0);
        var e2 = new EntityId(2, 0);
        var e3 = new EntityId(3, 0);

        archetype.AddEntity(e1);
        archetype.AddEntity(e2);
        archetype.AddEntity(e3);

        archetype.SetComponent(e1, new TestPosition(1, 0, 0));
        archetype.SetComponent(e2, new TestPosition(0, 2, 0));
        archetype.SetComponent(e3, new TestPosition(0, 0, 3));

        archetype.RemoveEntity(e1);

        Assert.That(archetype.ContainsEntity(e1), Is.False, "e1 应已被移除");
        Assert.That(archetype.ContainsEntity(e3), Is.True, "e3 应仍存在");

        ref var pos = ref archetype.GetComponent<TestPosition>(e3);
        Assert.That(pos.Z, Is.EqualTo(3f), "e3 的组件数据应正确");
    }
}

[TestFixture]
public class ArchetypeManagerTests
{
    private struct TestPosition
    {
        public float X;
        public float Y;
        public float Z;

        public TestPosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    private struct TestVelocity
    {
        public float Vx;
        public float Vy;

        public TestVelocity(float vx, float vy)
        {
            Vx = vx;
            Vy = vy;
        }
    }

    private struct TestHealth
    {
        public int Current;
        public int Max;

        public TestHealth(int current, int max)
        {
            Current = current;
            Max = max;
        }
    }

    private ArchetypeManager _manager;
    private EntityManager _entityManager;

    [SetUp]
    public void Setup()
    {
        var typeIdRegistry = new ComponentTypeId();
        _manager = new ArchetypeManager(typeIdRegistry);
        _entityManager = new EntityManager();
    }

    [Test]
    public void GetOrCreate_ReturnsSameArchetypeForSameTypes()
    {
        var a1 = _manager.GetOrCreate([typeof(TestPosition)]);
        var a2 = _manager.GetOrCreate([typeof(TestPosition)]);

        Assert.That(ReferenceEquals(a1, a2), Is.True, "相同类型应返回同一 Archetype");
    }

    [Test]
    public void GetOrCreate_DifferentTypes_ReturnsDifferentArchetypes()
    {
        var a1 = _manager.GetOrCreate([typeof(TestPosition)]);
        var a2 = _manager.GetOrCreate([typeof(TestVelocity)]);

        Assert.That(ReferenceEquals(a1, a2), Is.False, "不同类型应返回不同 Archetype");
    }

    [Test]
    public void AssignArchetype_EntityInArchetype()
    {
        var entity = _entityManager.CreateEntity();
        var archetype = _manager.GetOrCreate([typeof(TestPosition)]);

        _manager.AssignArchetype(entity, archetype);

        Assert.That(_manager.GetArchetypeForEntity(entity), Is.SameAs(archetype), "实体应属于指定 Archetype");
    }

    [Test]
    public void AddComponentType_MigratesEntity()
    {
        var entity = _entityManager.CreateEntity();
        var posArchetype = _manager.GetOrCreate([typeof(TestPosition)]);

        _manager.AssignArchetype(entity, posArchetype);
        posArchetype.SetComponent(entity, new TestPosition(1, 2, 3));

        var newArchetype = _manager.AddComponentType<TestVelocity>(entity);

        Assert.That(newArchetype.HasComponent<TestPosition>(), Is.True, "新 Archetype 应包含 TestPosition");
        Assert.That(newArchetype.HasComponent<TestVelocity>(), Is.True, "新 Archetype 应包含 TestVelocity");
        Assert.That(posArchetype.EntityCount, Is.EqualTo(0), "旧 Archetype 应为空");
        Assert.That(newArchetype.EntityCount, Is.EqualTo(1), "新 Archetype 应有 1 个实体");
    }

    [Test]
    public void AddComponentType_PreservesExistingData()
    {
        var entity = _entityManager.CreateEntity();
        var posArchetype = _manager.GetOrCreate([typeof(TestPosition)]);

        _manager.AssignArchetype(entity, posArchetype);
        posArchetype.SetComponent(entity, new TestPosition(10, 20, 30));

        var newArchetype = _manager.AddComponentType<TestVelocity>(entity);

        ref var pos = ref newArchetype.GetComponent<TestPosition>(entity);
        Assert.That(pos.X, Is.EqualTo(10f), "迁移后 X 应保持 10");
        Assert.That(pos.Y, Is.EqualTo(20f), "迁移后 Y 应保持 20");
        Assert.That(pos.Z, Is.EqualTo(30f), "迁移后 Z 应保持 30");
    }

    [Test]
    public void RemoveComponentType_MigratesEntity()
    {
        var entity = _entityManager.CreateEntity();
        var posVelArchetype = _manager.GetOrCreate([typeof(TestPosition), typeof(TestVelocity)]);

        _manager.AssignArchetype(entity, posVelArchetype);
        posVelArchetype.SetComponent(entity, new TestPosition(1, 2, 3));
        posVelArchetype.SetComponent(entity, new TestVelocity(4, 5));

        var newArchetype = _manager.RemoveComponentType<TestVelocity>(entity);

        Assert.That(newArchetype.HasComponent<TestPosition>(), Is.True, "新 Archetype 应包含 TestPosition");
        Assert.That(newArchetype.HasComponent<TestVelocity>(), Is.False, "新 Archetype 不应包含 TestVelocity");

        ref var pos = ref newArchetype.GetComponent<TestPosition>(entity);
        Assert.That(pos.X, Is.EqualTo(1f), "迁移后 X 应保持 1");
    }

    [Test]
    public void RemoveEntity_CleansUpArchetype()
    {
        var entity = _entityManager.CreateEntity();
        var archetype = _manager.GetOrCreate([typeof(TestPosition)]);

        _manager.AssignArchetype(entity, archetype);

        _manager.RemoveEntity(entity);

        Assert.That(_manager.GetArchetypeForEntity(entity), Is.Null, "移除后实体不应属于任何 Archetype");
        Assert.That(archetype.EntityCount, Is.EqualTo(0), "Archetype 应为空");
    }

    [Test]
    public void QueryArchetypes_FiltersCorrectly()
    {
        _manager.GetOrCreate([typeof(TestPosition)]);
        _manager.GetOrCreate([typeof(TestPosition), typeof(TestVelocity)]);
        _manager.GetOrCreate([typeof(TestPosition), typeof(TestVelocity), typeof(TestHealth)]);
        _manager.GetOrCreate([typeof(TestHealth)]);

        var allTypes = new HashSet<Type> { typeof(TestPosition), typeof(TestVelocity) };
        var noneTypes = new HashSet<Type> { typeof(TestHealth) };

        var results = _manager.QueryArchetypes(allTypes, new HashSet<Type>(), noneTypes).ToList();

        Assert.That(results.Count, Is.EqualTo(1), "应有 1 个匹配的 Archetype");
        Assert.That(results[0].HasComponent<TestPosition>(), Is.True, "应包含 TestPosition");
        Assert.That(results[0].HasComponent<TestVelocity>(), Is.True, "应包含 TestVelocity");
        Assert.That(results[0].HasComponent<TestHealth>(), Is.False, "不应包含 TestHealth");
    }
}

[TestFixture]
public class ChunkPoolTests
{
    private struct TestPosition
    {
        public float X;
        public float Y;
        public float Z;
    }

    [Test]
    public void Rent_ReturnsNewChunk()
    {
        var pool = new ChunkPool(new ComponentTypeId());
        var chunk = pool.Rent([typeof(TestPosition)]);

        Assert.That(chunk, Is.Not.Null, "Rent 应返回非空 Chunk");
        Assert.That(pool.TotalCount, Is.EqualTo(1), "TotalCount 应为 1");
    }

    [Test]
    public void Return_ChunkIsClearedAndReusable()
    {
        var pool = new ChunkPool(new ComponentTypeId());
        var chunk = pool.Rent([typeof(TestPosition)]);

        chunk.AddEntity(new EntityId(1, 0));
        pool.Return(chunk);

        Assert.That(chunk.Count, Is.EqualTo(0), "归还后 Chunk 应被清空");
        Assert.That(pool.AvailableCount, Is.EqualTo(1), "AvailableCount 应为 1");

        var reused = pool.Rent([typeof(TestPosition)]);
        Assert.That(ReferenceEquals(chunk, reused), Is.True, "应复用同一 Chunk");
    }

    [Test]
    public void Clear_ResetsPool()
    {
        var pool = new ChunkPool(new ComponentTypeId());
        pool.Rent([typeof(TestPosition)]);
        pool.Rent([typeof(TestPosition)]);

        pool.Clear();

        Assert.That(pool.TotalCount, Is.EqualTo(0), "Clear 后 TotalCount 应为 0");
        Assert.That(pool.AvailableCount, Is.EqualTo(0), "Clear 后 AvailableCount 应为 0");
    }
}

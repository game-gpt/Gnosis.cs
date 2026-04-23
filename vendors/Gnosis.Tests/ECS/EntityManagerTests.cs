using Gnosis.ECS.Entity;
using NUnit.Framework;

namespace Gnosis.ECS;

[TestFixture]
public class EntityManagerTests
{
    private EntityManager _manager;

    [SetUp]
    public void Setup()
    {
        _manager = new EntityManager();
    }

    [Test]
    public void CreateEntity_ReturnsValidEntityId()
    {
        var entityId = _manager.CreateEntity();

        Assert.That(entityId.Index, Is.GreaterThan(0), "实体索引应大于 0");
        Assert.That(_manager.IsAlive(entityId), Is.True, "新创建的实体应为活跃状态");
    }

    [Test]
    public void CreateEntity_IncrementsAliveCount()
    {
        _manager.CreateEntity();
        _manager.CreateEntity();
        _manager.CreateEntity();

        Assert.That(_manager.AliveCount, Is.EqualTo(3), "创建 3 个实体后 AliveCount 应为 3");
    }

    [Test]
    public void DestroyEntity_EntityNoLongerAlive()
    {
        var entityId = _manager.CreateEntity();

        _manager.DestroyEntity(entityId);

        Assert.That(_manager.IsAlive(entityId), Is.False, "销毁后实体应为非活跃状态");
        Assert.That(_manager.AliveCount, Is.EqualTo(0), "销毁后 AliveCount 应为 0");
    }

    [Test]
    public void DestroyEntity_GenerationIncrements()
    {
        var entityId = _manager.CreateEntity();
        var originalGeneration = entityId.Generation;

        _manager.DestroyEntity(entityId);

        var newEntityId = _manager.CreateEntity();
        Assert.That(newEntityId.Index, Is.EqualTo(entityId.Index), "新实体应复用旧索引");
        Assert.That(newEntityId.Generation, Is.EqualTo(originalGeneration + 1), "新实体代际应递增");
    }

    [Test]
    public void IsAlive_OldGenerationId_ReturnsFalse()
    {
        var entityId = _manager.CreateEntity();

        _manager.DestroyEntity(entityId);

        var oldEntityId = new EntityId(entityId.Index, entityId.Generation);
        Assert.That(_manager.IsAlive(oldEntityId), Is.False, "旧代际 ID 应返回 false");
    }

    [Test]
    public void IsAlive_NullEntityId_ReturnsFalse()
    {
        Assert.That(_manager.IsAlive(EntityId.Null), Is.False, "Null EntityId 应返回 false");
    }

    [Test]
    public void CreateEntities_ReturnsCorrectCount()
    {
        var entities = _manager.CreateEntities(5);

        Assert.That(entities.Length, Is.EqualTo(5), "应创建 5 个实体");
        Assert.That(_manager.AliveCount, Is.EqualTo(5), "AliveCount 应为 5");

        foreach (var entityId in entities)
        {
            Assert.That(_manager.IsAlive(entityId), Is.True, "每个实体应为活跃状态");
        }
    }

    [Test]
    public void CreateEntities_NegativeCount_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _manager.CreateEntities(-1));
    }

    [Test]
    public void DestroyEntity_NonExistent_ReturnsFalse()
    {
        var fakeEntityId = new EntityId(9999, 0);

        var result = _manager.DestroyEntity(fakeEntityId);

        Assert.That(result, Is.False, "销毁不存在的实体应返回 false");
    }

    [Test]
    public void DestroyEntity_AlreadyDestroyed_ReturnsFalse()
    {
        var entityId = _manager.CreateEntity();

        _manager.DestroyEntity(entityId);
        var result = _manager.DestroyEntity(entityId);

        Assert.That(result, Is.False, "重复销毁应返回 false");
    }

    [Test]
    public void IndexReuse_PrioritizesFreeList()
    {
        var entity1 = _manager.CreateEntity();
        var entity2 = _manager.CreateEntity();
        var entity3 = _manager.CreateEntity();

        _manager.DestroyEntity(entity2);

        var newEntity = _manager.CreateEntity();

        Assert.That(newEntity.Index, Is.EqualTo(entity2.Index), "新实体应复用 entity2 的索引");
        Assert.That(newEntity.Generation, Is.EqualTo(entity2.Generation + 1), "新实体代际应递增");
    }

    [Test]
    public void AliveCount_AfterMixedOperations()
    {
        var e1 = _manager.CreateEntity();
        var e2 = _manager.CreateEntity();
        var e3 = _manager.CreateEntity();

        Assert.That(_manager.AliveCount, Is.EqualTo(3), "创建 3 个后应为 3");

        _manager.DestroyEntity(e2);
        Assert.That(_manager.AliveCount, Is.EqualTo(2), "销毁 1 个后应为 2");

        _manager.CreateEntity();
        Assert.That(_manager.AliveCount, Is.EqualTo(3), "再创建 1 个后应为 3");

        _manager.DestroyEntity(e1);
        _manager.DestroyEntity(e3);
        Assert.That(_manager.AliveCount, Is.EqualTo(1), "再销毁 2 个后应为 1");
    }

    [Test]
    public void GetGeneration_ReturnsCorrectGeneration()
    {
        var entityId = _manager.CreateEntity();
        var generation = _manager.GetGeneration(entityId.Index);

        Assert.That(generation, Is.EqualTo(entityId.Generation), "GetGeneration 应返回正确代际");
    }

    [Test]
    public void GetGeneration_OutOfRange_ReturnsZero()
    {
        var generation = _manager.GetGeneration(99999);

        Assert.That(generation, Is.EqualTo(0u), "超出范围的索引应返回 0");
    }

    [Test]
    public void EntityId_Null_IsNullProperty()
    {
        Assert.That(EntityId.Null.IsNull, Is.True, "Null 的 IsNull 应为 true");
        Assert.That(EntityId.Null.Index, Is.EqualTo(0u), "Null 的 Index 应为 0");
        Assert.That(EntityId.Null.Generation, Is.EqualTo(0u), "Null 的 Generation 应为 0");
    }

    [Test]
    public void EntityId_NonNull_IsNullFalse()
    {
        var entityId = _manager.CreateEntity();

        Assert.That(entityId.IsNull, Is.False, "非 Null 的 IsNull 应为 false");
    }
}

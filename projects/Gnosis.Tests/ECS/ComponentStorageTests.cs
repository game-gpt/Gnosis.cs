using Gnosis.Core;
using Gnosis.ECS;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.ECS;

[TestFixture]
public class ComponentStorageTests : GnosisTester
{
    private ComponentStorage _storage;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _storage = new ComponentStorage();
    }

    [Test]
    public void GetPool_CreatesPoolLazily()
    {
        var pool = _storage.GetPool<Position>();

        Assert.That(pool, Is.Not.Null, "GetPool 应返回非空池实例");
        Assert.That(pool.ComponentType, Is.EqualTo(typeof(Position)), "池的组件类型应为 Position");
    }

    [Test]
    public void GetPool_ReturnsSameInstance()
    {
        var pool1 = _storage.GetPool<Position>();
        var pool2 = _storage.GetPool<Position>();

        Assert.That(ReferenceEquals(pool1, pool2), Is.True, "多次 GetPool 应返回同一实例");
    }

    [Test]
    public void GetPool_DifferentTypes_ReturnsDifferentInstances()
    {
        var posPool = _storage.GetPool<Position>();
        var playerIdPool = _storage.GetPool<PlayerId>();

        Assert.That(ReferenceEquals(posPool, playerIdPool), Is.False, "不同类型应返回不同池实例");
    }

    [Test]
    public void GetArchetypeStorage_SingleType_ReturnsMatchingEntities()
    {
        var entity1 = EntityId.New();
        var entity2 = EntityId.New();
        var posPool = _storage.GetPool<Position>();

        posPool.Add(entity1, new Position(1.0f, 0.0f, 0.0f));
        posPool.Add(entity2, new Position(0.0f, 1.0f, 0.0f));

        var archetype = _storage.GetArchetypeStorage(typeof(Position));

        Assert.That(archetype.EntityCount, Is.EqualTo(2), "Archetype 应包含 2 个实体");
        Assert.That(archetype.HasComponent<Position>(), Is.True, "Archetype 应包含 Position 组件类型");
    }

    [Test]
    public void GetArchetypeStorage_MultipleTypes_IntersectsEntities()
    {
        var entity1 = EntityId.New();
        var entity2 = EntityId.New();
        var entity3 = EntityId.New();
        var posPool = _storage.GetPool<Position>();
        var playerIdPool = _storage.GetPool<PlayerId>();

        posPool.Add(entity1, new Position(1.0f, 0.0f, 0.0f));
        posPool.Add(entity2, new Position(0.0f, 1.0f, 0.0f));
        posPool.Add(entity3, new Position(0.0f, 0.0f, 1.0f));

        playerIdPool.Add(entity1, PlayerId.New());
        playerIdPool.Add(entity3, PlayerId.New());

        var archetype = _storage.GetArchetypeStorage(typeof(Position), typeof(PlayerId));

        Assert.That(archetype.EntityCount, Is.EqualTo(2), "同时拥有 Position 和 PlayerId 的实体应为 2 个");
    }

    [Test]
    public void GetArchetypeStorage_EmptyTypeList_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _storage.GetArchetypeStorage());
    }

    [Test]
    public void GetArchetypeStorage_NonExistentType_ReturnsEmptyArchetype()
    {
        var archetype = _storage.GetArchetypeStorage(typeof(Position));

        Assert.That(archetype.EntityCount, Is.EqualTo(0), "不存在的组件类型应返回空 Archetype");
    }

    [Test]
    public void Archetype_HasComponent_ReturnsCorrectResult()
    {
        var posPool = _storage.GetPool<Position>();
        posPool.Add(EntityId.New(), new Position(1.0f, 0.0f, 0.0f));

        var archetype = _storage.GetArchetypeStorage(typeof(Position));

        Assert.That(archetype.HasComponent<Position>(), Is.True, "应包含 Position 类型");
        Assert.That(archetype.HasComponent<PlayerId>(), Is.False, "不应包含 PlayerId 类型");
    }

    [Test]
    public void Archetype_ComponentTypes_ContainsCorrectTypes()
    {
        var posPool = _storage.GetPool<Position>();
        posPool.Add(EntityId.New(), new Position(1.0f, 0.0f, 0.0f));

        var archetype = _storage.GetArchetypeStorage(typeof(Position), typeof(PlayerId));

        Assert.That(archetype.ComponentTypes.Contains(typeof(Position)), Is.True, "ComponentTypes 应包含 Position");
        Assert.That(archetype.ComponentTypes.Contains(typeof(PlayerId)), Is.True, "ComponentTypes 应包含 PlayerId");
    }
}

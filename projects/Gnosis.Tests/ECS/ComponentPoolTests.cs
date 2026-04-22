using Gnosis.Core;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.ECS;

[TestFixture]
public class ComponentPoolTests : GnosisTester
{
    private ComponentPool<Position> _pool;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _pool = new ComponentPool<Position>();
    }

    [Test]
    public void AddAndGet_ReturnsCorrectComponent()
    {
        var entityId = EntityId.New();
        var position = new Position(1.0f, 2.0f, 3.0f);

        _pool.Add(entityId, position);
        var result = _pool.Get<Position>(entityId);

        Assert.That(result, Is.EqualTo(position), "获取的组件应与添加的组件一致");
    }

    [Test]
    public void Add_SameEntityReplacesComponent()
    {
        var entityId = EntityId.New();
        var position1 = new Position(1.0f, 2.0f, 3.0f);
        var position2 = new Position(4.0f, 5.0f, 6.0f);

        _pool.Add(entityId, position1);
        _pool.Add(entityId, position2);

        var result = _pool.Get<Position>(entityId);
        Assert.That(result, Is.EqualTo(position2), "重复添加应替换旧组件");
        Assert.That(_pool.Count, Is.EqualTo(1), "重复添加后数量应为 1");
    }

    [Test]
    public void Remove_ComponentNoLongerAccessible()
    {
        var entityId = EntityId.New();
        var position = new Position(1.0f, 2.0f, 3.0f);

        _pool.Add(entityId, position);
        _pool.Remove<Position>(entityId);

        Assert.That(_pool.Has<Position>(entityId), Is.False, "移除后 Has 应返回 false");
        Assert.That(_pool.Count, Is.EqualTo(0), "移除后数量应为 0");
    }

    [Test]
    public void Has_ReturnsTrueWhenComponentExists()
    {
        var entityId = EntityId.New();
        _pool.Add(entityId, new Position(1.0f, 2.0f, 3.0f));

        Assert.That(_pool.Has<Position>(entityId), Is.True, "存在组件时 Has 应返回 true");
    }

    [Test]
    public void Has_ReturnsFalseWhenComponentNotExists()
    {
        var entityId = EntityId.New();

        Assert.That(_pool.Has<Position>(entityId), Is.False, "不存在组件时 Has 应返回 false");
    }

    [Test]
    public void Has_ReturnsFalseForWrongType()
    {
        var entityId = EntityId.New();
        _pool.Add(entityId, new Position(1.0f, 2.0f, 3.0f));

        Assert.That(_pool.Has<PlayerId>(entityId), Is.False, "类型不匹配时 Has 应返回 false");
    }

    [Test]
    public void GetAll_ReturnsAllComponents()
    {
        var entity1 = EntityId.New();
        var entity2 = EntityId.New();
        var entity3 = EntityId.New();
        var pos1 = new Position(1.0f, 0.0f, 0.0f);
        var pos2 = new Position(0.0f, 1.0f, 0.0f);
        var pos3 = new Position(0.0f, 0.0f, 1.0f);

        _pool.Add(entity1, pos1);
        _pool.Add(entity2, pos2);
        _pool.Add(entity3, pos3);

        var all = _pool.GetAll();
        Assert.That(all.Count, Is.EqualTo(3), "GetAll 应返回 3 个组件");
    }

    [Test]
    public void GetAllEntityIds_ReturnsAllEntityIds()
    {
        var entity1 = EntityId.New();
        var entity2 = EntityId.New();

        _pool.Add(entity1, new Position(1.0f, 0.0f, 0.0f));
        _pool.Add(entity2, new Position(0.0f, 1.0f, 0.0f));

        var ids = _pool.GetAllEntityIds();
        Assert.That(ids.Count, Is.EqualTo(2), "GetAllEntityIds 应返回 2 个实体 ID");
        Assert.That(ids, Does.Contain(entity1), "应包含 entity1");
        Assert.That(ids, Does.Contain(entity2), "应包含 entity2");
    }

    [Test]
    public void Count_ReturnsCorrectCount()
    {
        Assert.That(_pool.Count, Is.EqualTo(0), "初始数量应为 0");

        var entity1 = EntityId.New();
        var entity2 = EntityId.New();

        _pool.Add(entity1, new Position(1.0f, 0.0f, 0.0f));
        Assert.That(_pool.Count, Is.EqualTo(1), "添加 1 个后数量应为 1");

        _pool.Add(entity2, new Position(0.0f, 1.0f, 0.0f));
        Assert.That(_pool.Count, Is.EqualTo(2), "添加 2 个后数量应为 2");

        _pool.Remove<Position>(entity1);
        Assert.That(_pool.Count, Is.EqualTo(1), "移除 1 个后数量应为 1");
    }

    [Test]
    public void ComponentType_ReturnsCorrectType()
    {
        Assert.That(_pool.ComponentType, Is.EqualTo(typeof(Position)), "ComponentType 应返回 Position 类型");
    }

    [Test]
    public void Add_TypeMismatch_ThrowsInvalidOperationException()
    {
        var entityId = EntityId.New();

        Assert.Throws<InvalidOperationException>(() => _pool.Add(entityId, new PlayerId(Guid.NewGuid())));
    }

    [Test]
    public void Get_NonExistentEntity_ThrowsKeyNotFoundException()
    {
        var entityId = EntityId.New();

        Assert.Throws<KeyNotFoundException>(() => _pool.Get<Position>(entityId));
    }

    [Test]
    public void Remove_NonExistentEntity_DoesNotThrow()
    {
        var entityId = EntityId.New();

        Assert.DoesNotThrow(() => _pool.Remove<Position>(entityId));
    }

    [Test]
    public void Remove_MiddleEntity_MaintainsDenseArrayIntegrity()
    {
        var entity1 = EntityId.New();
        var entity2 = EntityId.New();
        var entity3 = EntityId.New();
        var pos1 = new Position(1.0f, 0.0f, 0.0f);
        var pos2 = new Position(0.0f, 1.0f, 0.0f);
        var pos3 = new Position(0.0f, 0.0f, 1.0f);

        _pool.Add(entity1, pos1);
        _pool.Add(entity2, pos2);
        _pool.Add(entity3, pos3);

        _pool.Remove<Position>(entity2);

        Assert.That(_pool.Has<Position>(entity1), Is.True, "entity1 应仍存在");
        Assert.That(_pool.Has<Position>(entity3), Is.True, "entity3 应仍存在");
        Assert.That(_pool.Has<Position>(entity2), Is.False, "entity2 应已移除");
        Assert.That(_pool.Count, Is.EqualTo(2), "移除后数量应为 2");
    }

    [Test]
    public void GetAll_AfterRemove_ContainsCorrectComponents()
    {
        var entity1 = EntityId.New();
        var entity2 = EntityId.New();
        var pos1 = new Position(1.0f, 0.0f, 0.0f);
        var pos2 = new Position(0.0f, 1.0f, 0.0f);

        _pool.Add(entity1, pos1);
        _pool.Add(entity2, pos2);
        _pool.Remove<Position>(entity1);

        var all = _pool.GetAll();
        Assert.That(all.Count, Is.EqualTo(1), "移除后 GetAll 应返回 1 个组件");
    }
}

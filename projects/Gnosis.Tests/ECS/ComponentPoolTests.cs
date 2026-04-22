using Gnosis.ECS.Component;
using Gnosis.ECS.Entity;
using NUnit.Framework;

namespace Gnosis.ECS;

[TestFixture]
public class ComponentPoolTests
{
    private ComponentPool<TestPosition> _pool;
    private EntityManager _entityManager;

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

    [SetUp]
    public void Setup()
    {
        _pool = new ComponentPool<TestPosition>();
        _entityManager = new EntityManager();
    }

    [Test]
    public void AddAndGet_ReturnsCorrectComponent()
    {
        var entityId = _entityManager.CreateEntity();
        var position = new TestPosition(1.0f, 2.0f, 3.0f);

        _pool.Add(entityId, position);
        var result = _pool.Get<TestPosition>(entityId);

        Assert.That(result.X, Is.EqualTo(1.0f), "X 应为 1.0");
        Assert.That(result.Y, Is.EqualTo(2.0f), "Y 应为 2.0");
        Assert.That(result.Z, Is.EqualTo(3.0f), "Z 应为 3.0");
    }

    [Test]
    public void Add_SameEntityReplacesComponent()
    {
        var entityId = _entityManager.CreateEntity();
        var position1 = new TestPosition(1.0f, 2.0f, 3.0f);
        var position2 = new TestPosition(4.0f, 5.0f, 6.0f);

        _pool.Add(entityId, position1);
        _pool.Add(entityId, position2);

        var result = _pool.Get<TestPosition>(entityId);
        Assert.That(result.X, Is.EqualTo(4.0f), "替换后 X 应为 4.0");
        Assert.That(_pool.Count, Is.EqualTo(1), "重复添加后数量应为 1");
    }

    [Test]
    public void Remove_ComponentNoLongerAccessible()
    {
        var entityId = _entityManager.CreateEntity();
        var position = new TestPosition(1.0f, 2.0f, 3.0f);

        _pool.Add(entityId, position);
        _pool.Remove<TestPosition>(entityId);

        Assert.That(_pool.Has<TestPosition>(entityId), Is.False, "移除后 Has 应返回 false");
        Assert.That(_pool.Count, Is.EqualTo(0), "移除后数量应为 0");
    }

    [Test]
    public void Has_ReturnsTrueWhenComponentExists()
    {
        var entityId = _entityManager.CreateEntity();
        _pool.Add(entityId, new TestPosition(1.0f, 2.0f, 3.0f));

        Assert.That(_pool.Has<TestPosition>(entityId), Is.True, "存在组件时 Has 应返回 true");
    }

    [Test]
    public void Has_ReturnsFalseWhenComponentNotExists()
    {
        var entityId = _entityManager.CreateEntity();

        Assert.That(_pool.Has<TestPosition>(entityId), Is.False, "不存在组件时 Has 应返回 false");
    }

    [Test]
    public void GetAll_ReturnsAllComponents()
    {
        var entity1 = _entityManager.CreateEntity();
        var entity2 = _entityManager.CreateEntity();
        var entity3 = _entityManager.CreateEntity();

        _pool.Add(entity1, new TestPosition(1.0f, 0.0f, 0.0f));
        _pool.Add(entity2, new TestPosition(0.0f, 1.0f, 0.0f));
        _pool.Add(entity3, new TestPosition(0.0f, 0.0f, 1.0f));

        var all = _pool.GetAll();
        Assert.That(all.Count, Is.EqualTo(3), "GetAll 应返回 3 个组件");
    }

    [Test]
    public void GetAllEntityIds_ReturnsAllEntityIds()
    {
        var entity1 = _entityManager.CreateEntity();
        var entity2 = _entityManager.CreateEntity();

        _pool.Add(entity1, new TestPosition(1.0f, 0.0f, 0.0f));
        _pool.Add(entity2, new TestPosition(0.0f, 1.0f, 0.0f));

        var ids = _pool.GetAllEntityIds();
        Assert.That(ids.Count, Is.EqualTo(2), "GetAllEntityIds 应返回 2 个实体 ID");
        Assert.That(ids, Does.Contain(entity1), "应包含 entity1");
        Assert.That(ids, Does.Contain(entity2), "应包含 entity2");
    }

    [Test]
    public void Count_ReturnsCorrectCount()
    {
        Assert.That(_pool.Count, Is.EqualTo(0), "初始数量应为 0");

        var entity1 = _entityManager.CreateEntity();
        var entity2 = _entityManager.CreateEntity();

        _pool.Add(entity1, new TestPosition(1.0f, 0.0f, 0.0f));
        Assert.That(_pool.Count, Is.EqualTo(1), "添加 1 个后数量应为 1");

        _pool.Add(entity2, new TestPosition(0.0f, 1.0f, 0.0f));
        Assert.That(_pool.Count, Is.EqualTo(2), "添加 2 个后数量应为 2");

        _pool.Remove<TestPosition>(entity1);
        Assert.That(_pool.Count, Is.EqualTo(1), "移除 1 个后数量应为 1");
    }

    [Test]
    public void ComponentType_ReturnsCorrectType()
    {
        Assert.That(_pool.ComponentType, Is.EqualTo(typeof(TestPosition)), "ComponentType 应返回 TestPosition 类型");
    }

    [Test]
    public void Add_TypeMismatch_ThrowsInvalidOperationException()
    {
        var entityId = _entityManager.CreateEntity();

        Assert.Throws<InvalidOperationException>(() => _pool.Add(entityId, 42));
    }

    [Test]
    public void Get_NonExistentEntity_ThrowsKeyNotFoundException()
    {
        var entityId = _entityManager.CreateEntity();

        Assert.Throws<KeyNotFoundException>(() => _pool.Get<TestPosition>(entityId));
    }

    [Test]
    public void Remove_NonExistentEntity_DoesNotThrow()
    {
        var entityId = _entityManager.CreateEntity();

        Assert.DoesNotThrow(() => _pool.Remove<TestPosition>(entityId));
    }

    [Test]
    public void Remove_MiddleEntity_MaintainsDenseArrayIntegrity()
    {
        var entity1 = _entityManager.CreateEntity();
        var entity2 = _entityManager.CreateEntity();
        var entity3 = _entityManager.CreateEntity();

        _pool.Add(entity1, new TestPosition(1.0f, 0.0f, 0.0f));
        _pool.Add(entity2, new TestPosition(0.0f, 1.0f, 0.0f));
        _pool.Add(entity3, new TestPosition(0.0f, 0.0f, 1.0f));

        _pool.Remove<TestPosition>(entity2);

        Assert.That(_pool.Has<TestPosition>(entity1), Is.True, "entity1 应仍存在");
        Assert.That(_pool.Has<TestPosition>(entity3), Is.True, "entity3 应仍存在");
        Assert.That(_pool.Has<TestPosition>(entity2), Is.False, "entity2 应已移除");
        Assert.That(_pool.Count, Is.EqualTo(2), "移除后数量应为 2");
    }

    [Test]
    public void GetRef_ReturnsMutableReference()
    {
        var entityId = _entityManager.CreateEntity();
        _pool.Add(entityId, new TestPosition(1.0f, 2.0f, 3.0f));

        ref var pos = ref _pool.GetRef(entityId);
        pos.X = 10.0f;

        var result = _pool.Get<TestPosition>(entityId);
        Assert.That(result.X, Is.EqualTo(10.0f), "通过引用修改后 X 应为 10.0");
    }
}

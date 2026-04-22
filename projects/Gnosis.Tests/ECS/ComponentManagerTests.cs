using Gnosis.ECS.Component;
using Gnosis.ECS.Entity;
using NUnit.Framework;

namespace Gnosis.ECS;

[TestFixture]
public class ComponentManagerTests
{
    private ComponentManager _manager;
    private EntityManager _entityManager;

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

    [SetUp]
    public void Setup()
    {
        _manager = new ComponentManager();
        _entityManager = new EntityManager();
    }

    [Test]
    public void AddAndGet_ReturnsCorrectComponent()
    {
        var entityId = _entityManager.CreateEntity();
        var health = new TestHealth(100, 100);

        _manager.Add(entityId, health);
        var result = _manager.Get<TestHealth>(entityId);

        Assert.That(result.Current, Is.EqualTo(100), "Current 应为 100");
        Assert.That(result.Max, Is.EqualTo(100), "Max 应为 100");
    }

    [Test]
    public void Has_ReturnsTrueWhenComponentExists()
    {
        var entityId = _entityManager.CreateEntity();
        _manager.Add(entityId, new TestHealth(50, 100));

        Assert.That(_manager.Has<TestHealth>(entityId), Is.True, "存在组件时 Has 应返回 true");
    }

    [Test]
    public void Has_ReturnsFalseWhenComponentNotExists()
    {
        var entityId = _entityManager.CreateEntity();

        Assert.That(_manager.Has<TestHealth>(entityId), Is.False, "不存在组件时 Has 应返回 false");
    }

    [Test]
    public void Remove_ComponentNoLongerAccessible()
    {
        var entityId = _entityManager.CreateEntity();
        _manager.Add(entityId, new TestHealth(100, 100));

        _manager.Remove<TestHealth>(entityId);

        Assert.That(_manager.Has<TestHealth>(entityId), Is.False, "移除后 Has 应返回 false");
    }

    [Test]
    public void Get_NonExistentComponent_ThrowsKeyNotFoundException()
    {
        var entityId = _entityManager.CreateEntity();

        Assert.Throws<KeyNotFoundException>(() => _manager.Get<TestHealth>(entityId));
    }

    [Test]
    public void GetRef_ReturnsMutableReference()
    {
        var entityId = _entityManager.CreateEntity();
        _manager.Add(entityId, new TestHealth(100, 100));

        ref var health = ref _manager.GetRef<TestHealth>(entityId);
        health.Current = 50;

        var result = _manager.Get<TestHealth>(entityId);
        Assert.That(result.Current, Is.EqualTo(50), "通过引用修改后 Current 应为 50");
    }

    [Test]
    public void MultipleComponentTypes_WorkIndependently()
    {
        var entityId = _entityManager.CreateEntity();
        _manager.Add(entityId, new TestHealth(100, 100));
        _manager.Add(entityId, new TestVelocity(1.0f, 2.0f));

        Assert.That(_manager.Has<TestHealth>(entityId), Is.True, "应有 Health 组件");
        Assert.That(_manager.Has<TestVelocity>(entityId), Is.True, "应有 Velocity 组件");

        var health = _manager.Get<TestHealth>(entityId);
        var velocity = _manager.Get<TestVelocity>(entityId);

        Assert.That(health.Current, Is.EqualTo(100), "Health.Current 应为 100");
        Assert.That(velocity.Vx, Is.EqualTo(1.0f), "Velocity.Vx 应为 1.0");
    }

    [Test]
    public void OnEntityDestroyed_RemovesAllComponents()
    {
        var entityId = _entityManager.CreateEntity();
        _manager.Add(entityId, new TestHealth(100, 100));
        _manager.Add(entityId, new TestVelocity(1.0f, 2.0f));

        _manager.OnEntityDestroyed(entityId);

        Assert.That(_manager.Has<TestHealth>(entityId), Is.False, "销毁后不应有 Health 组件");
        Assert.That(_manager.Has<TestVelocity>(entityId), Is.False, "销毁后不应有 Velocity 组件");
    }

    [Test]
    public void PoolCount_ReflectsRegisteredTypes()
    {
        Assert.That(_manager.PoolCount, Is.EqualTo(0), "初始池数量应为 0");

        var entityId = _entityManager.CreateEntity();
        _manager.Add(entityId, new TestHealth(100, 100));

        Assert.That(_manager.PoolCount, Is.EqualTo(1), "注册 1 种类型后池数量应为 1");

        _manager.Add(entityId, new TestVelocity(1.0f, 2.0f));

        Assert.That(_manager.PoolCount, Is.EqualTo(2), "注册 2 种类型后池数量应为 2");
    }

    [Test]
    public void Remove_NonExistentType_DoesNotThrow()
    {
        var entityId = _entityManager.CreateEntity();

        Assert.DoesNotThrow(() => _manager.Remove<TestHealth>(entityId));
    }
}

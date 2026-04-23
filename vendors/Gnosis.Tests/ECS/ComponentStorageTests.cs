using Gnosis.ECS.Component;
using Gnosis.ECS.Entity;
using NUnit.Framework;

namespace Gnosis.ECS;

[TestFixture]
public class ComponentStorageTests
{
    private ComponentStorage _storage;
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
        _storage = new ComponentStorage();
        _entityManager = new EntityManager();
    }

    [Test]
    public void GetPool_CreatesPoolLazily()
    {
        var pool = _storage.GetPool<TestPosition>();

        Assert.That(pool, Is.Not.Null, "GetPool 应返回非空池实例");
        Assert.That(pool.ComponentType, Is.EqualTo(typeof(TestPosition)), "池的组件类型应为 TestPosition");
    }

    [Test]
    public void GetPool_ReturnsSameInstance()
    {
        var pool1 = _storage.GetPool<TestPosition>();
        var pool2 = _storage.GetPool<TestPosition>();

        Assert.That(ReferenceEquals(pool1, pool2), Is.True, "多次 GetPool 应返回同一实例");
    }

    [Test]
    public void GetPool_DifferentTypes_ReturnsDifferentInstances()
    {
        var posPool = _storage.GetPool<TestPosition>();
        var velPool = _storage.GetPool<TestVelocity>();

        Assert.That(ReferenceEquals(posPool, velPool), Is.False, "不同类型应返回不同池实例");
    }

    [Test]
    public void GetArchetypeStorage_SingleType_ReturnsMatchingEntities()
    {
        var entity1 = _entityManager.CreateEntity();
        var entity2 = _entityManager.CreateEntity();
        var posPool = _storage.GetPool<TestPosition>();

        posPool.Add(entity1, new TestPosition(1.0f, 0.0f, 0.0f));
        posPool.Add(entity2, new TestPosition(0.0f, 1.0f, 0.0f));

        var archetype = _storage.GetArchetypeStorage(typeof(TestPosition));

        Assert.That(archetype.EntityCount, Is.EqualTo(2), "Archetype 应包含 2 个实体");
        Assert.That(archetype.HasComponent<TestPosition>(), Is.True, "Archetype 应包含 TestPosition 组件类型");
    }

    [Test]
    public void GetArchetypeStorage_MultipleTypes_IntersectsEntities()
    {
        var entity1 = _entityManager.CreateEntity();
        var entity2 = _entityManager.CreateEntity();
        var entity3 = _entityManager.CreateEntity();
        var posPool = _storage.GetPool<TestPosition>();
        var velPool = _storage.GetPool<TestVelocity>();

        posPool.Add(entity1, new TestPosition(1.0f, 0.0f, 0.0f));
        posPool.Add(entity2, new TestPosition(0.0f, 1.0f, 0.0f));
        posPool.Add(entity3, new TestPosition(0.0f, 0.0f, 1.0f));

        velPool.Add(entity1, new TestVelocity(1.0f, 0.0f));
        velPool.Add(entity3, new TestVelocity(0.0f, 1.0f));

        var archetype = _storage.GetArchetypeStorage(typeof(TestPosition), typeof(TestVelocity));

        Assert.That(archetype.EntityCount, Is.EqualTo(2), "同时拥有 TestPosition 和 TestVelocity 的实体应为 2 个");
    }

    [Test]
    public void GetArchetypeStorage_EmptyTypeList_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _storage.GetArchetypeStorage());
    }

    [Test]
    public void GetArchetypeStorage_NonExistentType_ReturnsEmptyArchetype()
    {
        var archetype = _storage.GetArchetypeStorage(typeof(TestPosition));

        Assert.That(archetype.EntityCount, Is.EqualTo(0), "不存在的组件类型应返回空 Archetype");
    }

    [Test]
    public void Archetype_HasComponent_ReturnsCorrectResult()
    {
        var posPool = _storage.GetPool<TestPosition>();
        posPool.Add(_entityManager.CreateEntity(), new TestPosition(1.0f, 0.0f, 0.0f));

        var archetype = _storage.GetArchetypeStorage(typeof(TestPosition));

        Assert.That(archetype.HasComponent<TestPosition>(), Is.True, "应包含 TestPosition 类型");
        Assert.That(archetype.HasComponent<TestVelocity>(), Is.False, "不应包含 TestVelocity 类型");
    }

    [Test]
    public void Archetype_ComponentTypes_ContainsCorrectTypes()
    {
        var posPool = _storage.GetPool<TestPosition>();
        posPool.Add(_entityManager.CreateEntity(), new TestPosition(1.0f, 0.0f, 0.0f));

        var archetype = _storage.GetArchetypeStorage(typeof(TestPosition), typeof(TestVelocity));

        Assert.That(archetype.ComponentTypes.Contains(typeof(TestPosition)), Is.True, "ComponentTypes 应包含 TestPosition");
        Assert.That(archetype.ComponentTypes.Contains(typeof(TestVelocity)), Is.True, "ComponentTypes 应包含 TestVelocity");
    }
}

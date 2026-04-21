using NUnit.Framework;
using Gnosis.Core;
using Gnosis.ECS;
using Gnosis.Testing;
using Gnosis.Testing.Mocks;
using Gnosis.Testing.Generators;

namespace TestProject.ECS
{
    // ECS 系统测试类
    [TestFixture]
    public class EcsTests : TestBase
    {
        private MockWorld _world;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _world = MockFactory.CreateWorld();
        }

        [TearDown]
        public override void Teardown()
        {
            _world?.Clear();
            base.Teardown();
        }

        // 测试创建实体
        [Test]
        public void CreateEntity_ShouldGenerateUniqueId()
        {
            // Arrange & Act
            var entity1 = _world.CreateEntity();
            var entity2 = _world.CreateEntity();

            // Assert
            Assert.That(entity1.Id, Is.Not.EqualTo(entity2.Id));
        }

        // 测试创建多个实体
        [Test]
        public void CreateEntity_MultipleEntities_ShouldHaveDifferentIds()
        {
            // Arrange
            var count = 10;

            // Act
            var entities = MockFactory.CreateEntities(count);

            // Assert
            var ids = entities.Select(e => e.Id).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(count));
        }

        // 测试添加组件
        [Test]
        public void AddComponent_ShouldAddComponentToEntity()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var position = new Position(1.0f, 2.0f, 3.0f);

            // Act
            _world.AddComponent(entity, position);
            var result = _world.GetComponent<Position>(entity);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.X, Is.EqualTo(1.0f));
            Assert.That(result.Y, Is.EqualTo(2.0f));
            Assert.That(result.Z, Is.EqualTo(3.0f));
        }

        // 测试获取不存在的组件
        [Test]
        public void GetComponent_NonExistentComponent_ShouldReturnNull()
        {
            // Arrange
            var entity = _world.CreateEntity();

            // Act
            var result = _world.GetComponent<Position>(entity);

            // Assert
            Assert.That(result, Is.Null);
        }

        // 测试检查组件是否存在
        [Test]
        public void HasComponent_ExistingComponent_ShouldReturnTrue()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var position = new Position(1.0f, 2.0f, 3.0f);
            _world.AddComponent(entity, position);

            // Act
            var hasComponent = _world.HasComponent<Position>(entity);

            // Assert
            Assert.That(hasComponent, Is.True);
        }

        // 测试检查不存在的组件
        [Test]
        public void HasComponent_NonExistentComponent_ShouldReturnFalse()
        {
            // Arrange
            var entity = _world.CreateEntity();

            // Act
            var hasComponent = _world.HasComponent<Position>(entity);

            // Assert
            Assert.That(hasComponent, Is.False);
        }

        // 测试移除组件
        [Test]
        public void RemoveComponent_ExistingComponent_ShouldRemoveComponent()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var position = new Position(1.0f, 2.0f, 3.0f);
            _world.AddComponent(entity, position);

            // Act
            _world.RemoveComponent<Position>(entity);
            var hasComponent = _world.HasComponent<Position>(entity);

            // Assert
            Assert.That(hasComponent, Is.False);
        }

        // 测试销毁实体
        [Test]
        public void DestroyEntity_ExistingEntity_ShouldRemoveEntity()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var initialCount = _world.EntityCount;

            // Act
            _world.DestroyEntity(entity);

            // Assert
            Assert.That(_world.EntityCount, Is.EqualTo(initialCount - 1));
        }

        // 测试销毁实体时移除组件
        [Test]
        public void DestroyEntity_WithComponents_ShouldRemoveAllComponents()
        {
            // Arrange
            var entity = _world.CreateEntity();
            var position = new Position(1.0f, 2.0f, 3.0f);
            _world.AddComponent(entity, position);

            // Act
            _world.DestroyEntity(entity);

            // Assert
            Assert.That(_world.EntityCount, Is.EqualTo(0));
        }

        // 测试添加系统
        [Test]
        public void AddSystem_ShouldAddSystemToWorld()
        {
            // Arrange
            var system = new MockSystem();

            // Act
            _world.AddSystem(system);

            // Assert
            Assert.That(_world.SystemCount, Is.EqualTo(1));
        }

        // 测试移除系统
        [Test]
        public void RemoveSystem_ExistingSystem_ShouldRemoveSystem()
        {
            // Arrange
            var system = new MockSystem();
            _world.AddSystem(system);

            // Act
            _world.RemoveSystem(system);

            // Assert
            Assert.That(_world.SystemCount, Is.EqualTo(0));
        }

        // 测试更新世界
        [Test]
        public void Update_WithSystems_ShouldCallSystemUpdate()
        {
            // Arrange
            var system = new MockSystem();
            _world.AddSystem(system);

            // Act
            _world.Update(0.016f);

            // Assert
            Assert.That(system.UpdateCallCount, Is.EqualTo(1));
        }

        // 测试随机数据生成
        [Test]
        public void RandomData_GenerateRandomPositions_ShouldCreateValidPositions()
        {
            // Arrange & Act
            var positions = TestDataGenerator.RandomPositions(10);

            // Assert
            Assert.That(positions.Count, Is.EqualTo(10));
            foreach (var position in positions)
            {
                Assert.That(position.X, Is.InRange(-100.0f, 100.0f));
                Assert.That(position.Y, Is.InRange(-100.0f, 100.0f));
                Assert.That(position.Z, Is.InRange(-100.0f, 100.0f));
            }
        }

        // 测试实体相等性
        [Test]
        public void EntityEquality_SameId_ShouldBeEqual()
        {
            // Arrange
            var entity1 = new MockEntity(1);
            var entity2 = new MockEntity(1);

            // Act & Assert
            Assert.That(entity1, Is.EqualTo(entity2));
        }

        // 测试实体不相等
        [Test]
        public void EntityEquality_DifferentId_ShouldNotBeEqual()
        {
            // Arrange
            var entity1 = new MockEntity(1);
            var entity2 = new MockEntity(2);

            // Act & Assert
            Assert.That(entity1, Is.Not.EqualTo(entity2));
        }
    }

    // Mock 系统实现
    public class MockSystem : ISystem
    {
        public int UpdateCallCount { get; private set; }

        public void OnLoad()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            UpdateCallCount++;
        }

        public void OnUnload()
        {
        }
    }
}

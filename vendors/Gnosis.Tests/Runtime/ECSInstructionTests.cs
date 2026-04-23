using System.Text;
using Gnosis.ECS.Entity;
using Gnosis.ECS.World;
using Gnosis.IR.Instruction;
using Gnosis.Runtime.VM;
using NUnit.Framework;

namespace Gnosis.Tests.Runtime;

[TestFixture]
public class ECSInstructionTests
{
    #region 测试用组件类型

    public struct Position
    {
        public float X;
        public float Y;
    }

    public struct Velocity
    {
        public float Dx;
        public float Dy;
    }

    public struct Health
    {
        public int Value;
    }

    #endregion

    #region ComponentTypeRegistry 注册测试

    [Test]
    public void Register_ReturnsConsistentIndex()
    {
        var registry = new ComponentTypeRegistry();

        var idx1 = registry.Register<Position>();
        var idx2 = registry.Register<Position>();

        Assert.That(idx1, Is.EqualTo(idx2));
        Assert.That(registry.Count, Is.EqualTo(1));
    }

    [Test]
    public void Register_DifferentTypes_ReturnsDifferentIndices()
    {
        var registry = new ComponentTypeRegistry();

        var idx1 = registry.Register<Position>();
        var idx2 = registry.Register<Velocity>();

        Assert.That(idx1, Is.Not.EqualTo(idx2));
        Assert.That(registry.Count, Is.EqualTo(2));
    }

    [Test]
    public void GetType_ReturnsCorrectType()
    {
        var registry = new ComponentTypeRegistry();
        var idx = registry.Register<Position>();

        var type = registry.GetType(idx);

        Assert.That(type, Is.EqualTo(typeof(Position)));
    }

    [Test]
    public void GetClrType_ReturnsCorrectType()
    {
        var registry = new ComponentTypeRegistry();
        var idx = registry.Register<Velocity>();

        var type = registry.GetClrType(idx);

        Assert.That(type, Is.EqualTo(typeof(Velocity)));
    }

    [Test]
    public void GetIndex_ByType_ReturnsCorrectIndex()
    {
        var registry = new ComponentTypeRegistry();
        var idx = registry.Register<Health>();

        var result = registry.GetIndex(typeof(Health));

        Assert.That(result, Is.EqualTo(idx));
    }

    [Test]
    public void RegisterByName_SetsNameMapping()
    {
        var registry = new ComponentTypeRegistry();
        var idx = registry.RegisterByName("Health", typeof(Health));

        var result = registry.GetIndex("Health");

        Assert.That(result, Is.EqualTo(idx));
    }

    [Test]
    public void IsRegistered_WorksCorrectly()
    {
        var registry = new ComponentTypeRegistry();
        var idx = registry.Register<Position>();

        Assert.That(registry.IsRegistered(idx), Is.True);
        Assert.That(registry.IsRegistered(999), Is.False);
    }

    [Test]
    public void GetRequiredType_ThrowsWhenNotFound()
    {
        var registry = new ComponentTypeRegistry();

        Assert.Throws<VMRuntimeException>(() => registry.GetRequiredType(999));
    }

    #endregion

    #region ComponentTypeRegistry ECS 操作测试

    [Test]
    public void AddComponent_CreatesDefaultComponent()
    {
        var world = new World();
        var registry = new ComponentTypeRegistry();
        var idx = registry.Register<Position>();
        var entity = world.CreateEntity();

        registry.AddComponent(world, entity, idx);

        Assert.That(registry.HasComponent(world, entity, idx), Is.True);
    }

    [Test]
    public void HasComponent_ReturnsFalseWhenNotPresent()
    {
        var world = new World();
        var registry = new ComponentTypeRegistry();
        var idx = registry.Register<Position>();
        var entity = world.CreateEntity();

        Assert.That(registry.HasComponent(world, entity, idx), Is.False);
    }

    [Test]
    public void AddMultipleComponents_AllPresent()
    {
        var world = new World();
        var registry = new ComponentTypeRegistry();
        var posIdx = registry.Register<Position>();
        var velIdx = registry.Register<Velocity>();
        var entity = world.CreateEntity();

        registry.AddComponent(world, entity, posIdx);
        registry.AddComponent(world, entity, velIdx);

        Assert.That(registry.HasComponent(world, entity, posIdx), Is.True);
        Assert.That(registry.HasComponent(world, entity, velIdx), Is.True);
    }

    [Test]
    public void GetComponent_ReturnsNonNullValue()
    {
        var world = new World();
        var registry = new ComponentTypeRegistry();
        var idx = registry.Register<Position>();
        var entity = world.CreateEntity();
        registry.AddComponent(world, entity, idx);

        var result = registry.GetComponent(world, entity, idx);

        Assert.That(result.IsNull, Is.False);
    }

    [Test]
    public void GetComponent_UnregisteredType_ReturnsNull()
    {
        var world = new World();
        var registry = new ComponentTypeRegistry();
        var entity = world.CreateEntity();

        var result = registry.GetComponent(world, entity, 999);

        Assert.That(result.IsNull, Is.True);
    }

    [Test]
    public void RemoveComponent_RemovesFromEntity()
    {
        var world = new World();
        var registry = new ComponentTypeRegistry();
        var idx = registry.Register<Position>();
        var entity = world.CreateEntity();
        registry.AddComponent(world, entity, idx);

        registry.RemoveComponent(world, entity, idx);

        Assert.That(registry.HasComponent(world, entity, idx), Is.False);
    }

    #endregion

    #region VMInterpreter 构造函数测试

    [Test]
    public void VMInterpreter_WithWorld_StoresWorld()
    {
        var state = new VMState();
        var nativeRegistry = new NativeFunctionRegistry();
        var world = new World();
        var vm = new VMInterpreter(state, nativeRegistry, world);

        Assert.That(vm.IsRunning, Is.False);
    }

    [Test]
    public void VMInterpreter_WithComponentRegistry_StoresRegistry()
    {
        var state = new VMState();
        var nativeRegistry = new NativeFunctionRegistry();
        var componentRegistry = new ComponentTypeRegistry();
        var world = new World();
        var vm = new VMInterpreter(state, nativeRegistry, componentRegistry, world);

        Assert.That(vm.ComponentRegistry, Is.SameAs(componentRegistry));
    }

    #endregion
}

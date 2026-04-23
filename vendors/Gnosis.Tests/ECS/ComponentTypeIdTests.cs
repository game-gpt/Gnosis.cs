using Gnosis.ECS.Component;
using NUnit.Framework;

namespace Gnosis.ECS;

[TestFixture]
public class ComponentTypeIdTests
{
    private ComponentTypeId _typeId;

    private struct ComponentA
    {
        public int Value;
    }

    private struct ComponentB
    {
        public float Value;
    }

    private struct ComponentC
    {
        public string Name;
    }

    [SetUp]
    public void Setup()
    {
        _typeId = new ComponentTypeId();
    }

    [Test]
    public void GetOrRegister_AssignsIncrementalIds()
    {
        var idA = _typeId.GetOrRegister<ComponentA>();
        var idB = _typeId.GetOrRegister<ComponentB>();
        var idC = _typeId.GetOrRegister<ComponentC>();

        Assert.That(idA, Is.EqualTo(0), "第一个注册的类型 ID 应为 0");
        Assert.That(idB, Is.EqualTo(1), "第二个注册的类型 ID 应为 1");
        Assert.That(idC, Is.EqualTo(2), "第三个注册的类型 ID 应为 2");
    }

    [Test]
    public void GetOrRegister_SameTypeReturnsSameId()
    {
        var id1 = _typeId.GetOrRegister<ComponentA>();
        var id2 = _typeId.GetOrRegister<ComponentA>();

        Assert.That(id1, Is.EqualTo(id2), "同一类型多次注册应返回相同 ID");
    }

    [Test]
    public void Count_ReflectsRegisteredTypes()
    {
        Assert.That(_typeId.Count, Is.EqualTo(0), "初始数量应为 0");

        _typeId.GetOrRegister<ComponentA>();
        Assert.That(_typeId.Count, Is.EqualTo(1), "注册 1 种后应为 1");

        _typeId.GetOrRegister<ComponentB>();
        Assert.That(_typeId.Count, Is.EqualTo(2), "注册 2 种后应为 2");
    }

    [Test]
    public void GetId_ReturnsCorrectIdForRegisteredType()
    {
        var registeredId = _typeId.GetOrRegister<ComponentA>();
        var retrievedId = _typeId.GetId<ComponentA>();

        Assert.That(retrievedId, Is.EqualTo(registeredId), "GetId 应返回注册时的 ID");
    }

    [Test]
    public void GetId_ReturnsMinusOneForUnregisteredType()
    {
        var id = _typeId.GetId<ComponentA>();

        Assert.That(id, Is.EqualTo(-1), "未注册类型应返回 -1");
    }

    [Test]
    public void IsRegistered_ReturnsTrueForRegisteredType()
    {
        _typeId.GetOrRegister<ComponentA>();

        Assert.That(_typeId.IsRegistered<ComponentA>(), Is.True, "已注册类型应返回 true");
    }

    [Test]
    public void IsRegistered_ReturnsFalseForUnregisteredType()
    {
        Assert.That(_typeId.IsRegistered<ComponentA>(), Is.False, "未注册类型应返回 false");
    }

    [Test]
    public void GetType_ReturnsCorrectType()
    {
        var id = _typeId.GetOrRegister<ComponentA>();

        var type = _typeId.GetType(id);

        Assert.That(type, Is.EqualTo(typeof(ComponentA)), "GetType 应返回正确的类型");
    }

    [Test]
    public void GetType_UnregisteredId_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => _typeId.GetType(9999));
    }
}

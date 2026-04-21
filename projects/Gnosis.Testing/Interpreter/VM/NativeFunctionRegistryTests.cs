using Gnosis.Interpreter.VM;
using NUnit.Framework;

namespace Gnosis.Testing.Interpreter.VM;

/// <summary>
/// 原生函数注册表单元测试
/// </summary>
[TestFixture]
public class NativeFunctionRegistryTests : TestBase
{
    private class MockNativeFunction : INativeFunction
    {
        public int Id { get; }
        public string Name { get; }
        public int ParameterCount { get; }
        public object? Execute(IVMState vm, object?[] args) => null;

        public MockNativeFunction(int id, string name, int parameterCount = 0)
        {
            Id = id;
            Name = name;
            ParameterCount = parameterCount;
        }
    }

    #region 注册测试

    [Test]
    public void Register_AddsFunction()
    {
        var registry = new NativeFunctionRegistry();
        var func = new MockNativeFunction(1, "test_func");

        registry.Register(func);

        Assert.That(registry.Get(1), Is.SameAs(func));
        Assert.That(registry.Get("test_func"), Is.SameAs(func));
    }

    [Test]
    public void Register_DuplicateId_ThrowsException()
    {
        var registry = new NativeFunctionRegistry();
        registry.Register(new MockNativeFunction(1, "func_a"));

        AssertThrows<VMDuplicateFunctionException>(() =>
            registry.Register(new MockNativeFunction(1, "func_b")));
    }

    #endregion

    #region 注销测试

    [Test]
    public void UnregisterById_RemovesFunction()
    {
        var registry = new NativeFunctionRegistry();
        var func = new MockNativeFunction(1, "test_func");
        registry.Register(func);

        registry.Unregister(1);

        Assert.That(registry.Get(1), Is.Null);
        Assert.That(registry.Get("test_func"), Is.Null);
    }

    [Test]
    public void UnregisterByName_RemovesFunction()
    {
        var registry = new NativeFunctionRegistry();
        var func = new MockNativeFunction(1, "test_func");
        registry.Register(func);

        registry.Unregister("test_func");

        Assert.That(registry.Get(1), Is.Null);
        Assert.That(registry.Get("test_func"), Is.Null);
    }

    #endregion

    #region 查找测试

    [Test]
    public void GetById_ReturnsFunction()
    {
        var registry = new NativeFunctionRegistry();
        var func = new MockNativeFunction(1, "test_func");
        registry.Register(func);

        var result = registry.Get(1);

        Assert.That(result, Is.SameAs(func));
    }

    [Test]
    public void GetByName_ReturnsFunction()
    {
        var registry = new NativeFunctionRegistry();
        var func = new MockNativeFunction(1, "test_func");
        registry.Register(func);

        var result = registry.Get("test_func");

        Assert.That(result, Is.SameAs(func));
    }

    [Test]
    public void GetById_NotFound_ReturnsNull()
    {
        var registry = new NativeFunctionRegistry();

        var result = registry.Get(999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetByName_NotFound_ReturnsNull()
    {
        var registry = new NativeFunctionRegistry();

        var result = registry.Get("nonexistent");

        Assert.That(result, Is.Null);
    }

    #endregion

    #region 包含检查测试

    [Test]
    public void ContainsById_ReturnsCorrectResult()
    {
        var registry = new NativeFunctionRegistry();
        registry.Register(new MockNativeFunction(1, "test_func"));

        Assert.That(registry.Contains(1), Is.True);
        Assert.That(registry.Contains(999), Is.False);
    }

    [Test]
    public void ContainsByName_ReturnsCorrectResult()
    {
        var registry = new NativeFunctionRegistry();
        registry.Register(new MockNativeFunction(1, "test_func"));

        Assert.That(registry.Contains("test_func"), Is.True);
        Assert.That(registry.Contains("nonexistent"), Is.False);
    }

    #endregion

    #region 批量操作测试

    [Test]
    public void GetAll_ReturnsAllFunctions()
    {
        var registry = new NativeFunctionRegistry();
        var func1 = new MockNativeFunction(1, "func_a");
        var func2 = new MockNativeFunction(2, "func_b");
        registry.Register(func1);
        registry.Register(func2);

        var all = registry.GetAll();

        Assert.That(all.Count, Is.EqualTo(2));
        Assert.That(all, Does.Contain(func1));
        Assert.That(all, Does.Contain(func2));
    }

    [Test]
    public void Clear_RemovesAllFunctions()
    {
        var registry = new NativeFunctionRegistry();
        registry.Register(new MockNativeFunction(1, "func_a"));
        registry.Register(new MockNativeFunction(2, "func_b"));

        registry.Clear();

        Assert.That(registry.GetAll().Count, Is.EqualTo(0));
        Assert.That(registry.Contains(1), Is.False);
        Assert.That(registry.Contains("func_a"), Is.False);
    }

    #endregion
}

using Gnosis.Compiler;
using NUnit.Framework;

namespace Gnosis.Testing.Compiler;

/// <summary>
/// 能力注册表单元测试
/// </summary>
[TestFixture]
public class CapabilityRegistryTests
{
    private CapabilityRegistry _registry = null!;

    [SetUp]
    public void Setup()
    {
        _registry = new CapabilityRegistry();
    }

    /// <summary>
    /// 注册能力后，GetProvider 应返回对应的插件名称
    /// </summary>
    [Test]
    public void Register_AndGetProvider_ReturnsPluginName()
    {
        _registry.Register("Rendering", "VulkanPlugin");

        var provider = _registry.GetProvider("Rendering");

        Assert.That(provider, Is.EqualTo("VulkanPlugin"));
    }

    /// <summary>
    /// 查询未注册的能力时，GetProvider 应返回 null
    /// </summary>
    [Test]
    public void GetProvider_UnregisteredCapability_ReturnsNull()
    {
        var provider = _registry.GetProvider("NonExistent");

        Assert.That(provider, Is.Null);
    }

    /// <summary>
    /// 已注册的能力，Contains 应返回 true
    /// </summary>
    [Test]
    public void Contains_RegisteredCapability_ReturnsTrue()
    {
        _registry.Register("Physics", "PhysXPlugin");

        var result = _registry.Contains("Physics");

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// 未注册的能力，Contains 应返回 false
    /// </summary>
    [Test]
    public void Contains_UnregisteredCapability_ReturnsFalse()
    {
        var result = _registry.Contains("Unknown");

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// 注册多个能力后，GetAll 应返回所有注册项
    /// </summary>
    [Test]
    public void GetAll_ReturnsAllRegistrations()
    {
        _registry.Register("Rendering", "VulkanPlugin");
        _registry.Register("Physics", "PhysXPlugin");
        _registry.Register("Audio", "OpenALPlugin");

        var all = _registry.GetAll();

        Assert.That(all.Count, Is.EqualTo(3));
        Assert.That(all["Rendering"], Is.EqualTo("VulkanPlugin"));
        Assert.That(all["Physics"], Is.EqualTo("PhysXPlugin"));
        Assert.That(all["Audio"], Is.EqualTo("OpenALPlugin"));
    }

    /// <summary>
    /// 重复注册同一能力时，后注册的插件应覆盖先注册的
    /// </summary>
    [Test]
    public void Register_DuplicateCapability_OverwritesProvider()
    {
        _registry.Register("Rendering", "OpenGLPlugin");
        _registry.Register("Rendering", "VulkanPlugin");

        var provider = _registry.GetProvider("Rendering");

        Assert.That(provider, Is.EqualTo("VulkanPlugin"));
    }
}

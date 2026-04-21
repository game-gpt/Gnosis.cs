using Gnosis.Core;
using Gnosis.Core.Events;
using Gnosis.Infrastructure;
using NUnit.Framework;

namespace Gnosis.Testing.Core;

[TestFixture]
public class ConfigSystemTests
{
    private MockVirtualFileSystem _vfs = null!;
    private ConfigSystem _configSystem = null!;

    [SetUp]
    public void Setup()
    {
        _vfs = new MockVirtualFileSystem();
        _configSystem = new ConfigSystem(_vfs);
    }

    [TearDown]
    public void Teardown()
    {
        _configSystem.Shutdown();
    }

    #region 配置加载测试

    [Test]
    public void Load_ValidCfgFile_ParsesKeyValues()
    {
        _vfs.AddFile("config/default.cfg", @"
# 服务器配置
server.name=MyServer
server.port=8080
server.max_players=100
");

        _configSystem.Load("config/default.cfg");

        Assert.That(_configSystem.ContainsKey("server.name"), Is.True);
        Assert.That(_configSystem.ContainsKey("server.port"), Is.True);
        Assert.That(_configSystem.ContainsKey("server.max_players"), Is.True);
        Assert.That(_configSystem.Get<string>("server.name"), Is.EqualTo("MyServer"));
        Assert.That(_configSystem.Get<string>("server.port"), Is.EqualTo("8080"));
        Assert.That(_configSystem.Get<string>("server.max_players"), Is.EqualTo("100"));
    }

    [Test]
    public void Load_CfgWithCommentsAndBlankLines_IgnoresThem()
    {
        _vfs.AddFile("config/app.cfg", @"
# 这是注释

key1=value1

# 另一条注释
key2=value2
");

        _configSystem.Load("config/app.cfg");

        Assert.That(_configSystem.ContainsKey("key1"), Is.True);
        Assert.That(_configSystem.ContainsKey("key2"), Is.True);
        Assert.That(_configSystem.Get<string>("key1"), Is.EqualTo("value1"));
        Assert.That(_configSystem.Get<string>("key2"), Is.EqualTo("value2"));
    }

    [Test]
    public void Load_DuplicateKey_LastValueWins()
    {
        _vfs.AddFile("config/dup.cfg", @"
key=first
key=second
");

        _configSystem.Load("config/dup.cfg");

        Assert.That(_configSystem.Get<string>("key"), Is.EqualTo("second"));
    }

    [Test]
    public void Load_FileNotFound_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => _configSystem.Load("nonexistent.cfg"));
    }

    [Test]
    public void Load_InvalidLine_ThrowsFormatException()
    {
        _vfs.AddFile("config/bad.cfg", "no_equals_sign_here");

        Assert.Throws<FormatException>(() => _configSystem.Load("config/bad.cfg"));
    }

    [Test]
    public void Load_MultipleFiles_LaterOverridesEarlier()
    {
        _vfs.AddFile("config/base.cfg", "host=localhost\nport=3000\ndebug=true");
        _vfs.AddFile("config/override.cfg", "port=8080\ndebug=false");

        _configSystem.Load("config/base.cfg");
        _configSystem.Load("config/override.cfg");

        Assert.That(_configSystem.Get<string>("host"), Is.EqualTo("localhost"));
        Assert.That(_configSystem.Get<string>("port"), Is.EqualTo("8080"));
        Assert.That(_configSystem.Get<string>("debug"), Is.EqualTo("false"));
    }

    [Test]
    public void Load_ValueWithEqualsSign_ParsedCorrectly()
    {
        _vfs.AddFile("config/eq.cfg", "connection_string=Server=localhost;Port=5432");

        _configSystem.Load("config/eq.cfg");

        Assert.That(_configSystem.Get<string>("connection_string"), Is.EqualTo("Server=localhost;Port=5432"));
    }

    [Test]
    public void Load_KeyWithSpaces_Trimmed()
    {
        _vfs.AddFile("config/spaces.cfg", "  key  =  value  ");

        _configSystem.Load("config/spaces.cfg");

        Assert.That(_configSystem.Get<string>("key"), Is.EqualTo("value"));
    }

    #endregion

    #region 键值读取测试

    [Test]
    public void Get_ExistingKey_ReturnsValue()
    {
        _vfs.AddFile("config/app.cfg", "greeting=hello");
        _configSystem.Load("config/app.cfg");

        Assert.That(_configSystem.Get<string>("greeting"), Is.EqualTo("hello"));
    }

    [Test]
    public void Get_NonExistingKey_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => _configSystem.Get<string>("nonexistent"));
    }

    [Test]
    public void Get_WithDefaultValue_NonExistingKey_ReturnsDefault()
    {
        Assert.That(_configSystem.Get("missing", "fallback"), Is.EqualTo("fallback"));
    }

    [Test]
    public void Get_WithDefaultValue_ExistingKey_ReturnsActualValue()
    {
        _vfs.AddFile("config/app.cfg", "name=Gnosis");
        _configSystem.Load("config/app.cfg");

        Assert.That(_configSystem.Get("name", "default"), Is.EqualTo("Gnosis"));
    }

    [Test]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        _vfs.AddFile("config/app.cfg", "exists=yes");
        _configSystem.Load("config/app.cfg");

        Assert.That(_configSystem.ContainsKey("exists"), Is.True);
    }

    [Test]
    public void ContainsKey_NonExistingKey_ReturnsFalse()
    {
        Assert.That(_configSystem.ContainsKey("nope"), Is.False);
    }

    #endregion

    #region 类型转换测试

    [Test]
    public void Get_IntValue_ConvertedCorrectly()
    {
        _vfs.AddFile("config/app.cfg", "count=42");
        _configSystem.Load("config/app.cfg");

        Assert.That(_configSystem.Get<int>("count"), Is.EqualTo(42));
    }

    [Test]
    public void Get_LongValue_ConvertedCorrectly()
    {
        _vfs.AddFile("config/app.cfg", "big=9999999999");
        _configSystem.Load("config/app.cfg");

        Assert.That(_configSystem.Get<long>("big"), Is.EqualTo(9999999999L));
    }

    [Test]
    public void Get_FloatValue_ConvertedCorrectly()
    {
        _vfs.AddFile("config/app.cfg", "ratio=3.14");
        _configSystem.Load("config/app.cfg");

        Assert.That(_configSystem.Get<float>("ratio"), Is.EqualTo(3.14f).Within(0.001f));
    }

    [Test]
    public void Get_DoubleValue_ConvertedCorrectly()
    {
        _vfs.AddFile("config/app.cfg", "precision=3.14159265358979");
        _configSystem.Load("config/app.cfg");

        Assert.That(_configSystem.Get<double>("precision"), Is.EqualTo(3.14159265358979).Within(0.0000001));
    }

    [Test]
    public void Get_BoolValue_ConvertedCorrectly()
    {
        _vfs.AddFile("config/app.cfg", "enabled=true\ndisabled=false");
        _configSystem.Load("config/app.cfg");

        Assert.That(_configSystem.Get<bool>("enabled"), Is.True);
        Assert.That(_configSystem.Get<bool>("disabled"), Is.False);
    }

    [Test]
    public void Get_DecimalValue_ConvertedCorrectly()
    {
        _vfs.AddFile("config/app.cfg", "price=19.99");
        _configSystem.Load("config/app.cfg");

        Assert.That(_configSystem.Get<decimal>("price"), Is.EqualTo(19.99m));
    }

    [Test]
    public void Get_InvalidConversion_ThrowsInvalidCastException()
    {
        _vfs.AddFile("config/app.cfg", "not_a_number=abc");
        _configSystem.Load("config/app.cfg");

        Assert.Throws<InvalidCastException>(() => _configSystem.Get<int>("not_a_number"));
    }

    [Test]
    public void Get_DefaultValueWithInvalidConversion_ThrowsInvalidCastException()
    {
        _vfs.AddFile("config/app.cfg", "bad_int=xyz");
        _configSystem.Load("config/app.cfg");

        Assert.Throws<InvalidCastException>(() => _configSystem.Get("bad_int", 0));
    }

    #endregion

    #region 运行时修改测试

    [Test]
    public void Set_NewKey_AddsValue()
    {
        _configSystem.Set("new_key", "new_value");

        Assert.That(_configSystem.Get<string>("new_key"), Is.EqualTo("new_value"));
    }

    [Test]
    public void Set_ExistingKey_UpdatesValue()
    {
        _vfs.AddFile("config/app.cfg", "version=1.0");
        _configSystem.Load("config/app.cfg");

        _configSystem.Set("version", "2.0");

        Assert.That(_configSystem.Get<string>("version"), Is.EqualTo("2.0"));
    }

    [Test]
    public void Set_TriggersOnConfigChanged()
    {
        ConfigChangedEvent? triggeredEvent = null;
        _configSystem.OnConfigChanged += e => triggeredEvent = e;

        _configSystem.Set("test_key", "test_value");

        Assert.That(triggeredEvent, Is.Not.Null);
        Assert.That(triggeredEvent!.Key, Is.EqualTo("test_key"));
        Assert.That(triggeredEvent.OldValue, Is.Null);
        Assert.That(triggeredEvent.NewValue, Is.EqualTo("test_value"));
    }

    [Test]
    public void Set_UpdateExistingKey_EventContainsOldValue()
    {
        _vfs.AddFile("config/app.cfg", "level=1");
        _configSystem.Load("config/app.cfg");

        ConfigChangedEvent? triggeredEvent = null;
        _configSystem.OnConfigChanged += e => triggeredEvent = e;

        _configSystem.Set("level", "2");

        Assert.That(triggeredEvent, Is.Not.Null);
        Assert.That(triggeredEvent!.Key, Is.EqualTo("level"));
        Assert.That(triggeredEvent.OldValue, Is.EqualTo("1"));
        Assert.That(triggeredEvent.NewValue, Is.EqualTo("2"));
    }

    [Test]
    public void Set_NoEventHandler_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _configSystem.Set("key", "value"));
    }

    [Test]
    public void Set_NewKey_SourceIsRuntime()
    {
        _configSystem.Set("runtime_key", "value");

        Assert.That(_configSystem.GetSource("runtime_key"), Is.EqualTo("runtime"));
    }

    #endregion

    #region 配置层级覆盖测试

    [Test]
    public void Load_MultipleFiles_LaterFileOverridesSource()
    {
        _vfs.AddFile("config/base.cfg", "setting=base_value");
        _vfs.AddFile("config/prod.cfg", "setting=prod_value");

        _configSystem.Load("config/base.cfg");
        _configSystem.Load("config/prod.cfg");

        Assert.That(_configSystem.Get<string>("setting"), Is.EqualTo("prod_value"));
        Assert.That(_configSystem.GetSource("setting"), Is.EqualTo("config/prod.cfg"));
    }

    [Test]
    public void Load_MultipleFiles_NonOverriddenKeysRetained()
    {
        _vfs.AddFile("config/base.cfg", "base_only=retained\nshared=from_base");
        _vfs.AddFile("config/prod.cfg", "shared=from_prod");

        _configSystem.Load("config/base.cfg");
        _configSystem.Load("config/prod.cfg");

        Assert.That(_configSystem.Get<string>("base_only"), Is.EqualTo("retained"));
        Assert.That(_configSystem.Get<string>("shared"), Is.EqualTo("from_prod"));
    }

    #endregion

    #region 来源查询测试

    [Test]
    public void GetSource_ExistingKey_ReturnsSourcePath()
    {
        _vfs.AddFile("config/app.cfg", "key=value");
        _configSystem.Load("config/app.cfg");

        Assert.That(_configSystem.GetSource("key"), Is.EqualTo("config/app.cfg"));
    }

    [Test]
    public void GetSource_NonExistingKey_ReturnsNull()
    {
        Assert.That(_configSystem.GetSource("missing"), Is.Null);
    }

    [Test]
    public void GetSource_OverriddenKey_ReturnsLatestSource()
    {
        _vfs.AddFile("config/default.cfg", "timeout=30");
        _vfs.AddFile("config/custom.cfg", "timeout=60");

        _configSystem.Load("config/default.cfg");
        _configSystem.Load("config/custom.cfg");

        Assert.That(_configSystem.GetSource("timeout"), Is.EqualTo("config/custom.cfg"));
    }

    [Test]
    public void GetSource_RuntimeSetKey_ReturnsRuntime()
    {
        _configSystem.Set("dynamic", "value");

        Assert.That(_configSystem.GetSource("dynamic"), Is.EqualTo("runtime"));
    }

    #endregion

    #region 生命周期测试

    [Test]
    public void Shutdown_ClearsAllConfigurations()
    {
        _vfs.AddFile("config/app.cfg", "key=value");
        _configSystem.Load("config/app.cfg");

        _configSystem.Shutdown();

        Assert.That(_configSystem.ContainsKey("key"), Is.False);
    }

    [Test]
    public void Initialize_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _configSystem.Initialize());
    }

    #endregion

    #region ConfigChangedEvent 测试

    [Test]
    public void ConfigChangedEvent_PropertiesSetCorrectly()
    {
        var aggregateId = EntityId.New();
        var evt = new ConfigChangedEvent(aggregateId, "test.key", "old", "new");

        Assert.That(evt.Key, Is.EqualTo("test.key"));
        Assert.That(evt.OldValue, Is.EqualTo("old"));
        Assert.That(evt.NewValue, Is.EqualTo("new"));
        Assert.That(evt.AggregateId, Is.EqualTo(aggregateId));
        Assert.That(evt.OccurredOn, Is.Not.Null);
    }

    [Test]
    public void ConfigChangedEvent_NullOldValue_WhenNewKey()
    {
        var evt = new ConfigChangedEvent(EntityId.New(), "new_key", null, "value");

        Assert.That(evt.OldValue, Is.Null);
        Assert.That(evt.NewValue, Is.EqualTo("value"));
    }

    [Test]
    public void ConfigChangedEvent_IsDomainEvent()
    {
        var evt = new ConfigChangedEvent(EntityId.New(), "key", null, "val");

        Assert.That(evt, Is.InstanceOf<IDomainEvent>());
    }

    #endregion
}

/// <summary>
/// 虚拟文件系统 Mock 实现，用于测试
/// </summary>
public class MockVirtualFileSystem : IVirtualFileSystem
{
    private readonly Dictionary<string, byte[]> _files = new();
    private readonly Dictionary<string, IFileSystem> _mounts = new();

    public void AddFile(string path, string content)
    {
        _files[path] = System.Text.Encoding.UTF8.GetBytes(content);
    }

    public void Mount(string path, IFileSystem fileSystem)
    {
        _mounts[path] = fileSystem;
    }

    public void Unmount(string path)
    {
        _mounts.Remove(path);
    }

    public Stream? OpenRead(string path)
    {
        return _files.TryGetValue(path, out var data) ? new MemoryStream(data) : null;
    }

    public Stream? OpenWrite(string path)
    {
        var stream = new MemoryStream();
        return stream;
    }

    public bool FileExists(string path)
    {
        return _files.ContainsKey(path);
    }

    public bool DirectoryExists(string path)
    {
        return _files.Keys.Any(k => k.StartsWith(path));
    }

    public void CreateDirectory(string path)
    {
    }

    public void DeleteFile(string path)
    {
        _files.Remove(path);
    }

    public IEnumerable<string> GetFiles(string path, string searchPattern = "*")
    {
        return _files.Keys.Where(k => k.StartsWith(path));
    }

    public IEnumerable<string> GetDirectories(string path)
    {
        return Enumerable.Empty<string>();
    }
}

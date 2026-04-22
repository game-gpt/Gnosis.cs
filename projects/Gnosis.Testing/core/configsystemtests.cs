using Gnosis.Assets.VFS;
using Gnosis.Core;
using Gnosis.Core.Events;
using NUnit.Framework;

namespace Gnosis.Testing.Core;

[TestFixture]
public class ConfigSystemTests
{
    private ConfigSystem _configSystem = null!;
    private MockVirtualFileSystem _vfs = null!;

    [SetUp]
    public void SetUp()
    {
        _vfs = new MockVirtualFileSystem();
        _configSystem = new ConfigSystem(_vfs);
    }

    #region 加载配置

    [Test]
    public void Load_ParsesValidConfigFile()
    {
        _vfs.AddFile("config.cfg", "name=test\nvalue=42\nenabled=true");

        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.Get<string>("name"), Is.EqualTo("test"));
        Assert.That(_configSystem.Get<int>("value"), Is.EqualTo(42));
        Assert.That(_configSystem.Get<bool>("enabled"), Is.True);
    }

    [Test]
    public void Load_IgnoresCommentsAndEmptyLines()
    {
        _vfs.AddFile("config.cfg", "# 这是注释\n\nkey=value\n# 另一个注释");

        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.ContainsKey("key"), Is.True);
        Assert.That(_configSystem.Get<string>("key"), Is.EqualTo("value"));
    }

    [Test]
    public void Load_DuplicateKeyOverwrites()
    {
        _vfs.AddFile("config.cfg", "key=first\nkey=second");

        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.Get<string>("key"), Is.EqualTo("second"));
    }

    [Test]
    public void Load_FileNotFound_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => _configSystem.Load("nonexistent.cfg"));
    }

    [Test]
    public void Load_InvalidFormat_ThrowsFormatException()
    {
        _vfs.AddFile("config.cfg", "no_equals_sign");

        Assert.Throws<FormatException>(() => _configSystem.Load("config.cfg"));
    }

    [Test]
    public void Load_ValueContainsEquals()
    {
        _vfs.AddFile("config.cfg", "url=http://example.com?key=val");

        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.Get<string>("url"), Is.EqualTo("http://example.com?key=val"));
    }

    [Test]
    public void Load_TrimsKeyAndValue()
    {
        _vfs.AddFile("config.cfg", "  key  =  value  ");

        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.Get<string>("key"), Is.EqualTo("value"));
    }

    #endregion

    #region 配置层级

    [Test]
    public void Load_LaterFileOverridesEarlier()
    {
        _vfs.AddFile("base.cfg", "key=base\nonly_base=yes");
        _vfs.AddFile("override.cfg", "key=override\nonly_override=yes");

        _configSystem.Load("base.cfg");
        _configSystem.Load("override.cfg");

        Assert.That(_configSystem.Get<string>("key"), Is.EqualTo("override"));
        Assert.That(_configSystem.Get<string>("only_base"), Is.EqualTo("yes"));
        Assert.That(_configSystem.Get<string>("only_override"), Is.EqualTo("yes"));
    }

    #endregion

    #region 读取配置值

    [Test]
    public void Get_KeyExists_ReturnsValue()
    {
        _vfs.AddFile("config.cfg", "name=test");
        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.Get<string>("name"), Is.EqualTo("test"));
    }

    [Test]
    public void Get_KeyNotFound_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => _configSystem.Get<string>("nonexistent"));
    }

    [Test]
    public void Get_WithDefaultValue_KeyNotFound_ReturnsDefault()
    {
        var result = _configSystem.Get("nonexistent", "default");
        Assert.That(result, Is.EqualTo("default"));
    }

    [Test]
    public void Get_WithDefaultValue_KeyExists_ReturnsValue()
    {
        _vfs.AddFile("config.cfg", "key=value");
        _configSystem.Load("config.cfg");

        var result = _configSystem.Get("key", "default");
        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public void ContainsKey_ReturnsCorrectResult()
    {
        _vfs.AddFile("config.cfg", "key=value");
        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.ContainsKey("key"), Is.True);
        Assert.That(_configSystem.ContainsKey("nonexistent"), Is.False);
    }

    #endregion

    #region 类型转换

    [Test]
    public void Get_ConvertsToInt()
    {
        _vfs.AddFile("config.cfg", "count=42");
        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.Get<int>("count"), Is.EqualTo(42));
    }

    [Test]
    public void Get_ConvertsToFloat()
    {
        _vfs.AddFile("config.cfg", "ratio=3.14");
        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.Get<float>("ratio"), Is.EqualTo(3.14f).Within(0.001f));
    }

    [Test]
    public void Get_ConvertsToDouble()
    {
        _vfs.AddFile("config.cfg", "ratio=3.14159265358979");
        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.Get<double>("ratio"), Is.EqualTo(3.14159265358979).Within(0.0001));
    }

    [Test]
    public void Get_ConvertsToBool()
    {
        _vfs.AddFile("config.cfg", "enabled=true\ndisabled=false");
        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.Get<bool>("enabled"), Is.True);
        Assert.That(_configSystem.Get<bool>("disabled"), Is.False);
    }

    [Test]
    public void Get_ConvertsToLong()
    {
        _vfs.AddFile("config.cfg", "big=9999999999");
        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.Get<long>("big"), Is.EqualTo(9999999999L));
    }

    [Test]
    public void Get_InvalidConversion_ThrowsInvalidCastException()
    {
        _vfs.AddFile("config.cfg", "not_int=hello");
        _configSystem.Load("config.cfg");

        Assert.Throws<InvalidCastException>(() => _configSystem.Get<int>("not_int"));
    }

    #endregion

    #region 运行时修改

    [Test]
    public void Set_AddsNewKey()
    {
        _configSystem.Set("new_key", "new_value");

        Assert.That(_configSystem.Get<string>("new_key"), Is.EqualTo("new_value"));
    }

    [Test]
    public void Set_UpdatesExistingKey()
    {
        _vfs.AddFile("config.cfg", "key=old");
        _configSystem.Load("config.cfg");

        _configSystem.Set("key", "new");

        Assert.That(_configSystem.Get<string>("key"), Is.EqualTo("new"));
    }

    [Test]
    public void Set_TriggersConfigChangedEvent()
    {
        ConfigChangedEvent? receivedEvent = null;
        _configSystem.OnConfigChanged += e => receivedEvent = e;

        _configSystem.Set("key", "value");

        Assert.That(receivedEvent, Is.Not.Null);
        Assert.That(receivedEvent!.Key, Is.EqualTo("key"));
        Assert.That(receivedEvent.NewValue, Is.EqualTo("value"));
    }

    [Test]
    public void Set_EventContainsOldValue()
    {
        _vfs.AddFile("config.cfg", "key=old");
        _configSystem.Load("config.cfg");

        ConfigChangedEvent? receivedEvent = null;
        _configSystem.OnConfigChanged += e => receivedEvent = e;

        _configSystem.Set("key", "new");

        Assert.That(receivedEvent, Is.Not.Null);
        Assert.That(receivedEvent!.OldValue, Is.EqualTo("old"));
        Assert.That(receivedEvent.NewValue, Is.EqualTo("new"));
    }

    [Test]
    public void Set_NewKey_OldValueIsNull()
    {
        ConfigChangedEvent? receivedEvent = null;
        _configSystem.OnConfigChanged += e => receivedEvent = e;

        _configSystem.Set("new_key", "value");

        Assert.That(receivedEvent, Is.Not.Null);
        Assert.That(receivedEvent!.OldValue, Is.Null);
    }

    [Test]
    public void Set_NoHandler_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _configSystem.Set("key", "value"));
    }

    #endregion

    #region 来源查询

    [Test]
    public void GetSource_ReturnsFilePath()
    {
        _vfs.AddFile("config.cfg", "key=value");
        _configSystem.Load("config.cfg");

        Assert.That(_configSystem.GetSource("key"), Is.EqualTo("config.cfg"));
    }

    [Test]
    public void GetSource_NonexistentKey_ReturnsNull()
    {
        Assert.That(_configSystem.GetSource("nonexistent"), Is.Null);
    }

    [Test]
    public void GetSource_OverriddenKey_ReturnsLatestSource()
    {
        _vfs.AddFile("base.cfg", "key=base");
        _vfs.AddFile("override.cfg", "key=override");

        _configSystem.Load("base.cfg");
        _configSystem.Load("override.cfg");

        Assert.That(_configSystem.GetSource("key"), Is.EqualTo("override.cfg"));
    }

    [Test]
    public void GetSource_RuntimeSet_ReturnsRuntime()
    {
        _configSystem.Set("key", "value");

        Assert.That(_configSystem.GetSource("key"), Is.EqualTo("runtime"));
    }

    #endregion

    #region 生命周期

    [Test]
    public void Shutdown_ClearsAllConfig()
    {
        _vfs.AddFile("config.cfg", "key=value");
        _configSystem.Load("config.cfg");

        _configSystem.Shutdown();

        Assert.That(_configSystem.ContainsKey("key"), Is.False);
    }

    [Test]
    public void Initialize_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _configSystem.Initialize());
    }

    #endregion

    #region 辅助类

    private class MockVirtualFileSystem : IVirtualFileSystem
    {
        private readonly Dictionary<string, string> _files = new();

        public void AddFile(string path, string content)
        {
            _files[path] = content;
        }

        public void Mount(string path, IFileSystem fileSystem)
        {
        }

        public void Unmount(string path)
        {
        }

        public Stream? OpenRead(string path)
        {
            if (_files.TryGetValue(path, out var content))
            {
                return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            }
            return null;
        }

        public Stream? OpenWrite(string path)
        {
            return new MemoryStream();
        }

        public bool FileExists(string path)
        {
            return _files.ContainsKey(path);
        }

        public bool DirectoryExists(string path)
        {
            return false;
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
            return [];
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            return [];
        }
    }

    #endregion
}

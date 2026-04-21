using Gnosis.Compiler;
using Gnosis.Compiler.AST;
using NUnit.Framework;

namespace Gnosis.Testing.Compiler;

/// <summary>
/// 插件加载器单元测试，验证架构兼容性检查、宏与能力注册、重复加载防护等行为
/// </summary>
[TestFixture]
public class PluginLoaderTests
{
    #region Fields

    private PluginLoader _loader = null!;
    private MacroTable _macroTable = null!;
    private CapabilityRegistry _capabilityRegistry = null!;

    #endregion

    #region Setup

    [SetUp]
    public void Setup()
    {
        _loader = new PluginLoader();
        _macroTable = new MacroTable();
        _capabilityRegistry = new CapabilityRegistry();
    }

    #endregion

    #region Helper Methods

    private static PluginDecl CreatePlugin(
        string name,
        string[]? requiresArch = null,
        string[]? providesMacros = null,
        string[]? providesCapabilities = null)
    {
        return new PluginDecl(
            null, name,
            requiresArch ?? Array.Empty<string>(),
            providesMacros ?? Array.Empty<string>(),
            providesCapabilities ?? Array.Empty<string>(),
            Array.Empty<FunctionDecl>());
    }

    #endregion

    #region LoadPlugin Tests

    [Test]
    public void LoadPlugin_CompatibleArch_RegistersMacrosAndCapabilities()
    {
        var plugin = CreatePlugin(
            "MathPlugin",
            requiresArch: ["X64"],
            providesMacros: ["vec_add"],
            providesCapabilities: ["simd"]);

        _loader.LoadPlugin(plugin, ArchTarget.X64, _macroTable, _capabilityRegistry);

        Assert.That(_macroTable.Contains("vec_add"), Is.True);
        Assert.That(_capabilityRegistry.Contains("simd"), Is.True);
        Assert.That(_capabilityRegistry.GetProvider("simd"), Is.EqualTo("MathPlugin"));
    }

    [Test]
    public void LoadPlugin_IncompatibleArch_SkipsPlugin()
    {
        var plugin = CreatePlugin(
            "ArmPlugin",
            requiresArch: ["ARM64"],
            providesMacros: ["neon_add"],
            providesCapabilities: ["neon"]);

        _loader.LoadPlugin(plugin, ArchTarget.X64, _macroTable, _capabilityRegistry);

        Assert.That(_macroTable.Contains("neon_add"), Is.False);
        Assert.That(_capabilityRegistry.Contains("neon"), Is.False);
        Assert.That(_loader.IsLoaded("ArmPlugin"), Is.False);
    }

    [Test]
    public void LoadPlugin_EmptyRequiresArch_LoadsOnAllArchs()
    {
        var plugin = CreatePlugin(
            "UniversalPlugin",
            providesMacros: ["debug"],
            providesCapabilities: ["logging"]);

        _loader.LoadPlugin(plugin, ArchTarget.WASM, _macroTable, _capabilityRegistry);

        Assert.That(_macroTable.Contains("debug"), Is.True);
        Assert.That(_capabilityRegistry.Contains("logging"), Is.True);
        Assert.That(_loader.IsLoaded("UniversalPlugin"), Is.True);
    }

    [Test]
    public void LoadPlugin_DuplicateName_ThrowsInvalidOperationException()
    {
        var plugin = CreatePlugin("DupPlugin", providesMacros: ["m1"]);

        _loader.LoadPlugin(plugin, ArchTarget.X64, _macroTable, _capabilityRegistry);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _loader.LoadPlugin(plugin, ArchTarget.X64, _macroTable, _capabilityRegistry);
        });

        Assert.That(ex!.Message, Does.Contain("DupPlugin"));
    }

    [Test]
    public void LoadPlugin_MultipleMacros_AllInjected()
    {
        var plugin = CreatePlugin(
            "MacroPlugin",
            providesMacros: ["vec_add", "vec_mul", "vec_dot"]);

        _loader.LoadPlugin(plugin, ArchTarget.X64, _macroTable, _capabilityRegistry);

        Assert.That(_macroTable.Contains("vec_add"), Is.True);
        Assert.That(_macroTable.Contains("vec_mul"), Is.True);
        Assert.That(_macroTable.Contains("vec_dot"), Is.True);
        Assert.That(_macroTable.Get("vec_add"), Is.EqualTo("1"));
        Assert.That(_macroTable.Get("vec_mul"), Is.EqualTo("1"));
        Assert.That(_macroTable.Get("vec_dot"), Is.EqualTo("1"));
    }

    [Test]
    public void LoadPlugin_MultipleCapabilities_AllRegistered()
    {
        var plugin = CreatePlugin(
            "CapPlugin",
            providesCapabilities: ["playback", "recording", "mixing"]);

        _loader.LoadPlugin(plugin, ArchTarget.X64, _macroTable, _capabilityRegistry);

        Assert.That(_capabilityRegistry.Contains("playback"), Is.True);
        Assert.That(_capabilityRegistry.Contains("recording"), Is.True);
        Assert.That(_capabilityRegistry.Contains("mixing"), Is.True);
        Assert.That(_capabilityRegistry.GetProvider("playback"), Is.EqualTo("CapPlugin"));
        Assert.That(_capabilityRegistry.GetProvider("recording"), Is.EqualTo("CapPlugin"));
        Assert.That(_capabilityRegistry.GetProvider("mixing"), Is.EqualTo("CapPlugin"));
    }

    [Test]
    public void LoadPlugin_ArchAliasX86_64_CompatibleWithX64()
    {
        var plugin = CreatePlugin(
            "AliasPlugin",
            requiresArch: ["x86_64"],
            providesMacros: ["sse2"]);

        _loader.LoadPlugin(plugin, ArchTarget.X64, _macroTable, _capabilityRegistry);

        Assert.That(_macroTable.Contains("sse2"), Is.True);
        Assert.That(_loader.IsLoaded("AliasPlugin"), Is.True);
    }

    [Test]
    public void LoadPlugin_UnknownArch_SkipsPluginWithRequirements()
    {
        var plugin = CreatePlugin(
            "StrictPlugin",
            requiresArch: ["X64"],
            providesMacros: ["avx2"]);

        _loader.LoadPlugin(plugin, ArchTarget.Unknown, _macroTable, _capabilityRegistry);

        Assert.That(_macroTable.Contains("avx2"), Is.False);
        Assert.That(_loader.IsLoaded("StrictPlugin"), Is.False);
    }

    #endregion

    #region IsLoaded Tests

    [Test]
    public void IsLoaded_AfterLoading_ReturnsTrue()
    {
        var plugin = CreatePlugin("LoadedPlugin");

        _loader.LoadPlugin(plugin, ArchTarget.X64, _macroTable, _capabilityRegistry);

        Assert.That(_loader.IsLoaded("LoadedPlugin"), Is.True);
    }

    [Test]
    public void IsLoaded_BeforeLoading_ReturnsFalse()
    {
        Assert.That(_loader.IsLoaded("NonExistentPlugin"), Is.False);
    }

    #endregion

    #region Clear Tests

    [Test]
    public void Clear_RemovesAllLoadedPlugins()
    {
        var plugin = CreatePlugin("ClearPlugin", providesMacros: ["m1"]);

        _loader.LoadPlugin(plugin, ArchTarget.X64, _macroTable, _capabilityRegistry);
        Assert.That(_loader.IsLoaded("ClearPlugin"), Is.True);

        _loader.Clear();

        Assert.That(_loader.IsLoaded("ClearPlugin"), Is.False);
    }

    #endregion
}

using Oak.Valkyrie.AST;

namespace Gnosis.Toolchain.ScriptCompiler;

public sealed class PluginLoader
{
    #region Fields

    private readonly HashSet<string> _loadedPlugins = new(StringComparer.Ordinal);

    #endregion

    #region Public Methods

    public void LoadPlugin(PluginDecl plugin, ArchTarget arch, IMacroTable macroTable, ICapabilityRegistry capabilityRegistry)
    {
        if (_loadedPlugins.Contains(plugin.Name))
        {
            throw new InvalidOperationException($"插件已加载，不允许重复加载: {plugin.Name}");
        }

        if (plugin.RequiresArch.Count > 0 && !IsArchCompatible(arch, plugin.RequiresArch))
        {
            return;
        }

        foreach (var macro in plugin.ProvidesMacros)
        {
            macroTable.Add(macro, "1");
        }

        foreach (var cap in plugin.ProvidesCapabilities)
        {
            capabilityRegistry.Register(cap, plugin.Name);
        }

        _loadedPlugins.Add(plugin.Name);
    }

    public bool IsLoaded(string pluginName)
    {
        return _loadedPlugins.Contains(pluginName);
    }

    public void Clear()
    {
        _loadedPlugins.Clear();
    }

    #endregion

    #region Private Methods

    private static bool IsArchCompatible(ArchTarget arch, IReadOnlyList<string> requiresArch)
    {
        if (arch == ArchTarget.Unknown)
        {
            return false;
        }

        var archAliases = GetArchAliases(arch);

        foreach (var required in requiresArch)
        {
            if (archAliases.Contains(required))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> GetArchAliases(ArchTarget arch)
    {
        return arch switch
        {
            ArchTarget.X64 => new HashSet<string>(StringComparer.Ordinal) { "x86_64", "X64", "x64" },
            ArchTarget.ARM64 => new HashSet<string>(StringComparer.Ordinal) { "ARM64", "arm64" },
            ArchTarget.WASM => new HashSet<string>(StringComparer.Ordinal) { "WASM", "wasm" },
            _ => new HashSet<string>(StringComparer.Ordinal)
        };
    }

    #endregion
}

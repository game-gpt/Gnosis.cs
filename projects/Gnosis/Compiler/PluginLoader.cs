using Gnosis.Compiler.AST;

namespace Gnosis.Compiler;

/// <summary>
/// 插件加载器，负责根据架构兼容性加载插件并注册宏和能力
/// </summary>
public sealed class PluginLoader
{
    #region Fields

    private readonly HashSet<string> _loadedPlugins = new(StringComparer.Ordinal);

    #endregion

    #region Public Methods

    /// <summary>
    /// 加载指定插件，将其提供的宏和能力注册到对应的表中
    /// </summary>
    /// <param name="plugin">要加载的插件声明</param>
    /// <param name="arch">当前目标架构</param>
    /// <param name="macroTable">宏注册表</param>
    /// <param name="capabilityRegistry">能力注册表</param>
    /// <exception cref="InvalidOperationException">插件已加载时抛出</exception>
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

    /// <summary>
    /// 检查指定名称的插件是否已加载
    /// </summary>
    /// <param name="pluginName">插件名称</param>
    /// <returns>已加载返回 true，否则返回 false</returns>
    public bool IsLoaded(string pluginName)
    {
        return _loadedPlugins.Contains(pluginName);
    }

    /// <summary>
    /// 清除所有已加载的插件记录
    /// </summary>
    public void Clear()
    {
        _loadedPlugins.Clear();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 判断当前架构是否满足插件要求的架构列表
    /// </summary>
    /// <param name="arch">当前目标架构</param>
    /// <param name="requiresArch">插件要求的架构标识列表</param>
    /// <returns>兼容返回 true，否则返回 false</returns>
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

    /// <summary>
    /// 获取指定架构对应的所有字符串标识
    /// </summary>
    /// <param name="arch">目标架构</param>
    /// <returns>该架构对应的字符串标识集合</returns>
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

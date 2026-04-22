namespace Gnosis.Plugin.Host;

/// <summary>
/// 插件宿主接口
/// </summary>
public interface IPluginHost
{
    /// <summary>
    /// 加载插件
    /// </summary>
    /// <param name="path">插件路径</param>
    /// <returns>已加载的插件实例</returns>
    IPlugin LoadPlugin(string path);

    /// <summary>
    /// 卸载插件
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    void UnloadPlugin(string pluginId);

    /// <summary>
    /// 启用插件
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    void EnablePlugin(string pluginId);

    /// <summary>
    /// 禁用插件
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    void DisablePlugin(string pluginId);

    /// <summary>
    /// 按ID查询插件
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    /// <returns>插件实例，未找到返回 null</returns>
    IPlugin? GetPlugin(string pluginId);

    /// <summary>
    /// 按ID查询上下文
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    /// <returns>插件上下文，未找到返回 null</returns>
    IPluginContext? GetPluginContext(string pluginId);

    /// <summary>
    /// 已加载插件
    /// </summary>
    IReadOnlyDictionary<string, IPlugin> LoadedPlugins { get; }

    /// <summary>
    /// 插件加载事件
    /// </summary>
    event EventHandler<PluginEventArgs>? PluginLoaded;

    /// <summary>
    /// 插件卸载事件
    /// </summary>
    event EventHandler<PluginEventArgs>? PluginUnloaded;

    /// <summary>
    /// 插件启用事件
    /// </summary>
    event EventHandler<PluginEventArgs>? PluginEnabled;

    /// <summary>
    /// 插件禁用事件
    /// </summary>
    event EventHandler<PluginEventArgs>? PluginDisabled;
}

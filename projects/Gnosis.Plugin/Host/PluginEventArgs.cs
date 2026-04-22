namespace Gnosis.Plugin.Host;

/// <summary>
/// 插件事件参数
/// </summary>
public class PluginEventArgs : EventArgs
{
    /// <summary>
    /// 插件唯一标识
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// 插件实例
    /// </summary>
    public IPlugin Plugin { get; }

    /// <summary>
    /// 初始化插件事件参数
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    /// <param name="plugin">插件实例</param>
    public PluginEventArgs(string pluginId, IPlugin plugin)
    {
        PluginId = pluginId;
        Plugin = plugin;
    }
}

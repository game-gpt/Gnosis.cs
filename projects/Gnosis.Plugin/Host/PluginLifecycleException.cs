namespace Gnosis.Plugin.Host;

/// <summary>
/// 插件生命周期异常
/// </summary>
public class PluginLifecycleException : Exception
{
    /// <summary>
    /// 插件唯一标识
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public PluginState CurrentState { get; }

    /// <summary>
    /// 目标状态
    /// </summary>
    public PluginState TargetState { get; }

    /// <summary>
    /// 初始化插件生命周期异常
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    /// <param name="current">当前状态</param>
    /// <param name="target">目标状态</param>
    public PluginLifecycleException(string pluginId, PluginState current, PluginState target)
        : base($"插件 {pluginId} 无法从 {current} 状态转换到 {target} 状态")
    {
        PluginId = pluginId;
        CurrentState = current;
        TargetState = target;
    }
}

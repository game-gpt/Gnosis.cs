namespace Gnosis.Plugin.Host;

/// <summary>
/// 插件热重载事件参数
/// </summary>
public class PluginHotReloadEventArgs : EventArgs
{
    #region 属性

    /// <summary>
    /// 插件标识
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// 旧插件实例
    /// </summary>
    public IPlugin OldPlugin { get; }

    /// <summary>
    /// 新插件实例
    /// </summary>
    public IPlugin? NewPlugin { get; }

    /// <summary>
    /// 是否热重载成功
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化插件热重载事件参数
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    /// <param name="oldPlugin">旧插件实例</param>
    /// <param name="newPlugin">新插件实例</param>
    /// <param name="success">是否热重载成功</param>
    /// <param name="errorMessage">错误消息</param>
    public PluginHotReloadEventArgs(
        string pluginId,
        IPlugin oldPlugin,
        IPlugin? newPlugin,
        bool success,
        string? errorMessage)
    {
        PluginId = pluginId;
        OldPlugin = oldPlugin;
        NewPlugin = newPlugin;
        Success = success;
        ErrorMessage = errorMessage;
    }

    #endregion
}

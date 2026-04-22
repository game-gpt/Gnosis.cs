namespace Gnosis.Plugin.Isolation;

/// <summary>
/// 沙箱违规异常
/// </summary>
public class SandboxViolationException : Exception
{
    #region 属性

    /// <summary>
    /// 违规的插件标识
    /// </summary>
    public string PluginId { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用插件标识和异常消息初始化
    /// </summary>
    public SandboxViolationException(string pluginId, string message)
        : base(message)
    {
        PluginId = pluginId;
    }

    /// <summary>
    /// 使用插件标识、异常消息和内部异常初始化
    /// </summary>
    public SandboxViolationException(string pluginId, string message, Exception innerException)
        : base(message, innerException)
    {
        PluginId = pluginId;
    }

    #endregion
}

using System;

namespace Gnosis.Plugin.Loader;

/// <summary>
/// 插件加载异常
/// </summary>
public class PluginLoadException : Exception
{
    /// <summary>
    /// 出错的插件 ID
    /// </summary>
    public string? PluginId { get; }

    /// <summary>
    /// 出错的插件路径
    /// </summary>
    public string? PluginPath { get; }

    /// <summary>
    /// 使用异常消息初始化
    /// </summary>
    public PluginLoadException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用异常消息和内部异常初始化
    /// </summary>
    public PluginLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// 使用插件 ID、插件路径和异常消息初始化
    /// </summary>
    public PluginLoadException(string? pluginId, string? pluginPath, string message)
        : base(message)
    {
        PluginId = pluginId;
        PluginPath = pluginPath;
    }

    /// <summary>
    /// 使用插件 ID、插件路径、异常消息和内部异常初始化
    /// </summary>
    public PluginLoadException(string? pluginId, string? pluginPath, string message, Exception innerException)
        : base(message, innerException)
    {
        PluginId = pluginId;
        PluginPath = pluginPath;
    }
}

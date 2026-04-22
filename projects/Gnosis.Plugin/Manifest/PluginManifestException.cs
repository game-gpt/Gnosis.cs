using System;

namespace Gnosis.Plugin.Manifest;

/// <summary>
/// 插件清单解析异常
/// </summary>
public class PluginManifestException : Exception
{
    /// <summary>
    /// 出错的插件标识
    /// </summary>
    public string? PluginId { get; }

    /// <summary>
    /// 使用异常消息初始化
    /// </summary>
    public PluginManifestException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用异常消息和内部异常初始化
    /// </summary>
    public PluginManifestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// 使用插件标识和异常消息初始化
    /// </summary>
    public PluginManifestException(string pluginId, string message)
        : base(message)
    {
        PluginId = pluginId;
    }

    /// <summary>
    /// 使用插件标识、异常消息和内部异常初始化
    /// </summary>
    public PluginManifestException(string pluginId, string message, Exception innerException)
        : base(message, innerException)
    {
        PluginId = pluginId;
    }
}

using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Host;

/// <summary>
/// 插件上下文接口
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// 插件唯一标识
    /// </summary>
    string PluginId { get; }

    /// <summary>
    /// 清单引用
    /// </summary>
    PluginManifest Manifest { get; }

    /// <summary>
    /// 宿主引用
    /// </summary>
    IPluginHost Host { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    PluginState State { get; }

    /// <summary>
    /// 插件自定义数据存储
    /// </summary>
    IDictionary<string, object> Data { get; }
}

using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Host;

/// <summary>
/// 插件上下文实现
/// </summary>
public class PluginContext : IPluginContext
{
    /// <summary>
    /// 插件唯一标识
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// 清单引用
    /// </summary>
    public PluginManifest Manifest { get; }

    /// <summary>
    /// 宿主引用
    /// </summary>
    public IPluginHost Host { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public PluginState State { get; private set; }

    /// <summary>
    /// 插件自定义数据存储
    /// </summary>
    public IDictionary<string, object> Data { get; }

    /// <summary>
    /// 初始化插件上下文
    /// </summary>
    /// <param name="pluginId">插件唯一标识</param>
    /// <param name="manifest">清单引用</param>
    /// <param name="host">宿主引用</param>
    public PluginContext(string pluginId, PluginManifest manifest, IPluginHost host)
    {
        PluginId = pluginId;
        Manifest = manifest;
        Host = host;
        State = PluginState.Loaded;
        Data = new Dictionary<string, object>();
    }

    /// <summary>
    /// 设置插件状态（供 PluginHost 修改状态）
    /// </summary>
    /// <param name="state">目标状态</param>
    internal void SetState(PluginState state)
    {
        State = state;
    }
}

using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Host;

/// <summary>
/// 插件基础接口
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// 插件清单
    /// </summary>
    PluginManifest Manifest { get; }

    /// <summary>
    /// 加载时调用
    /// </summary>
    /// <param name="context">插件上下文</param>
    void OnLoad(IPluginContext context);

    /// <summary>
    /// 启用时调用
    /// </summary>
    void OnEnable();

    /// <summary>
    /// 禁用时调用
    /// </summary>
    void OnDisable();

    /// <summary>
    /// 卸载时调用
    /// </summary>
    void OnUnload();
}

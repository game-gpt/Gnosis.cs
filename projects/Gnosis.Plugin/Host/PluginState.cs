namespace Gnosis.Plugin.Host;

/// <summary>
/// 插件生命周期状态枚举
/// </summary>
public enum PluginState
{
    /// <summary>
    /// 未加载
    /// </summary>
    Unloaded,

    /// <summary>
    /// 已加载（清单已解析，资源已分配）
    /// </summary>
    Loaded,

    /// <summary>
    /// 已启用（正在运行）
    /// </summary>
    Enabled,

    /// <summary>
    /// 已禁用（已暂停，可重新启用）
    /// </summary>
    Disabled,

    /// <summary>
    /// 卸载中（正在清理资源）
    /// </summary>
    Unloading
}

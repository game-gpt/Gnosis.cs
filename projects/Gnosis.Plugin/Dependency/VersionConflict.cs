using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Dependency;

/// <summary>
/// 版本冲突描述
/// </summary>
public sealed class VersionConflict
{
    #region 属性

    /// <summary>
    /// 需要该依赖的插件标识
    /// </summary>
    public string RequiringPluginId { get; }

    /// <summary>
    /// 依赖插件标识
    /// </summary>
    public string DependencyPluginId { get; }

    /// <summary>
    /// 要求的版本范围
    /// </summary>
    public string RequiredVersionRange { get; }

    /// <summary>
    /// 实际安装的版本
    /// </summary>
    public PluginVersion ActualVersion { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化版本冲突描述
    /// </summary>
    public VersionConflict(
        string requiringPluginId,
        string dependencyPluginId,
        string requiredVersionRange,
        PluginVersion actualVersion)
    {
        RequiringPluginId = requiringPluginId;
        DependencyPluginId = dependencyPluginId;
        RequiredVersionRange = requiredVersionRange;
        ActualVersion = actualVersion;
    }

    #endregion

    #region 重写

    /// <summary>
    /// 返回版本冲突的描述字符串
    /// </summary>
    public override string ToString()
    {
        return $"插件 {RequiringPluginId} 需要 {DependencyPluginId}@{RequiredVersionRange}，但实际版本为 {ActualVersion}";
    }

    #endregion
}

namespace Gnosis.Plugin.Dependency;

/// <summary>
/// 缺失的依赖描述
/// </summary>
public sealed class MissingDependency
{
    #region 属性

    /// <summary>
    /// 需要该依赖的插件标识
    /// </summary>
    public string RequiringPluginId { get; }

    /// <summary>
    /// 缺失的依赖插件标识
    /// </summary>
    public string DependencyPluginId { get; }

    /// <summary>
    /// 要求的版本范围
    /// </summary>
    public string RequiredVersionRange { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化缺失依赖描述
    /// </summary>
    /// <param name="requiringPluginId">需要该依赖的插件标识</param>
    /// <param name="dependencyPluginId">缺失的依赖插件标识</param>
    /// <param name="requiredVersionRange">要求的版本范围</param>
    public MissingDependency(string requiringPluginId, string dependencyPluginId, string requiredVersionRange)
    {
        RequiringPluginId = requiringPluginId;
        DependencyPluginId = dependencyPluginId;
        RequiredVersionRange = requiredVersionRange;
    }

    #endregion

    #region 重写

    /// <summary>
    /// 返回缺失依赖的描述字符串
    /// </summary>
    public override string ToString()
    {
        return $"插件 {RequiringPluginId} 缺少依赖 {DependencyPluginId}@{RequiredVersionRange}";
    }

    #endregion
}

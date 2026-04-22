namespace Gnosis.Plugin.Dependency;

/// <summary>
/// 兼容性检查结果
/// </summary>
public sealed class CompatibilityResult
{
    #region 属性

    /// <summary>
    /// 插件标识
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// 是否兼容
    /// </summary>
    public bool IsCompatible { get; }

    /// <summary>
    /// 不兼容原因
    /// </summary>
    public string? Reason { get; }

    #endregion

    #region 构造函数

    private CompatibilityResult(string pluginId, bool isCompatible, string? reason)
    {
        PluginId = pluginId;
        IsCompatible = isCompatible;
        Reason = reason;
    }

    #endregion

    #region 工厂方法

    /// <summary>
    /// 创建兼容的结果
    /// </summary>
    public static CompatibilityResult Compatible(string pluginId)
    {
        return new CompatibilityResult(pluginId, true, null);
    }

    /// <summary>
    /// 创建不兼容的结果
    /// </summary>
    public static CompatibilityResult Incompatible(string pluginId, string reason)
    {
        return new CompatibilityResult(pluginId, false, reason);
    }

    #endregion
}

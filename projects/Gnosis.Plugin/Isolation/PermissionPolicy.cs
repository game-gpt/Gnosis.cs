namespace Gnosis.Plugin.Isolation;

/// <summary>
/// 权限策略，定义权限检查的行为模式
/// </summary>
public sealed class PermissionPolicy
{
    #region 属性

    /// <summary>
    /// 权限执行模式
    /// </summary>
    public PermissionEnforcementMode Mode { get; init; }

    /// <summary>
    /// 是否记录权限检查日志
    /// </summary>
    public bool EnableLogging { get; init; }

    /// <summary>
    /// 是否在权限拒绝时抛出异常
    /// </summary>
    public bool ThrowOnDeny { get; init; }

    #endregion

    #region 静态属性

    /// <summary>
    /// 默认权限策略
    /// </summary>
    public static PermissionPolicy Default => new()
    {
        Mode = PermissionEnforcementMode.Strict,
        EnableLogging = true,
        ThrowOnDeny = true
    };

    /// <summary>
    /// 宽松权限策略
    /// </summary>
    public static PermissionPolicy Permissive => new()
    {
        Mode = PermissionEnforcementMode.Permissive,
        EnableLogging = true,
        ThrowOnDeny = false
    };

    #endregion
}

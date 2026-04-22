namespace Gnosis.Plugin.Isolation;

/// <summary>
/// 权限检查状态
/// </summary>
public enum PermissionCheckStatus
{
    /// <summary>
    /// 权限已授予
    /// </summary>
    Granted,

    /// <summary>
    /// 权限已授予但带警告
    /// </summary>
    GrantedWithWarning,

    /// <summary>
    /// 权限已拒绝
    /// </summary>
    Denied
}

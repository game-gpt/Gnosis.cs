namespace Gnosis.Plugin.Isolation;

/// <summary>
/// 权限执行模式
/// </summary>
public enum PermissionEnforcementMode
{
    /// <summary>
    /// 严格模式：缺少权限即拒绝
    /// </summary>
    Strict,

    /// <summary>
    /// 宽松模式：缺少权限仅警告
    /// </summary>
    Permissive
}

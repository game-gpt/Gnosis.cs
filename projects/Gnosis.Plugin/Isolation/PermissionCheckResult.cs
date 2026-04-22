using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Isolation;

/// <summary>
/// 权限检查结果
/// </summary>
public sealed class PermissionCheckResult
{
    #region 属性

    /// <summary>
    /// 插件标识
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// 请求的权限
    /// </summary>
    public PluginPermission RequiredPermission { get; }

    /// <summary>
    /// 缺少的权限
    /// </summary>
    public PluginPermission MissingPermissions { get; }

    /// <summary>
    /// 检查状态
    /// </summary>
    public PermissionCheckStatus Status { get; }

    /// <summary>
    /// 结果消息
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// 是否通过
    /// </summary>
    public bool IsGranted => Status is PermissionCheckStatus.Granted or PermissionCheckStatus.GrantedWithWarning;

    #endregion

    #region 构造函数

    private PermissionCheckResult(
        string pluginId,
        PluginPermission required,
        PluginPermission missing,
        PermissionCheckStatus status,
        string? message)
    {
        PluginId = pluginId;
        RequiredPermission = required;
        MissingPermissions = missing;
        Status = status;
        Message = message;
    }

    #endregion

    #region 工厂方法

    /// <summary>
    /// 创建权限已授予的结果
    /// </summary>
    public static PermissionCheckResult Granted(string pluginId, PluginPermission required)
    {
        return new PermissionCheckResult(
            pluginId,
            required,
            PluginPermission.None,
            PermissionCheckStatus.Granted,
            null
        );
    }

    /// <summary>
    /// 创建权限已授予但带警告的结果
    /// </summary>
    public static PermissionCheckResult GrantedWithWarning(
        string pluginId,
        PluginPermission required,
        PluginPermission missing,
        string message)
    {
        return new PermissionCheckResult(
            pluginId,
            required,
            missing,
            PermissionCheckStatus.GrantedWithWarning,
            message
        );
    }

    /// <summary>
    /// 创建权限已拒绝的结果
    /// </summary>
    public static PermissionCheckResult Denied(
        string pluginId,
        PluginPermission required,
        PluginPermission missing,
        string message)
    {
        return new PermissionCheckResult(
            pluginId,
            required,
            missing,
            PermissionCheckStatus.Denied,
            message
        );
    }

    #endregion
}

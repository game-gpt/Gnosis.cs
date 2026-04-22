using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Isolation;

/// <summary>
/// 插件权限检查器，验证插件是否拥有执行特定操作的权限
/// </summary>
public sealed class PermissionChecker
{
    #region 字段

    private readonly Dictionary<string, PluginPermission> _customPermissions;

    #endregion

    #region 属性

    /// <summary>
    /// 默认权限策略
    /// </summary>
    public PermissionPolicy Policy { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用权限策略初始化权限检查器
    /// </summary>
    /// <param name="policy">权限策略，为 null 时使用默认策略</param>
    public PermissionChecker(PermissionPolicy? policy = null)
    {
        Policy = policy ?? PermissionPolicy.Default;
        _customPermissions = new Dictionary<string, PluginPermission>(StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 检查插件是否拥有指定权限
    /// </summary>
    /// <param name="manifest">插件清单</param>
    /// <param name="required">所需权限</param>
    /// <returns>检查结果</returns>
    public PermissionCheckResult Check(PluginManifest manifest, PluginPermission required)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var granted = manifest.Permissions;
        var hasPermission = (granted & required) == required;

        if (hasPermission)
        {
            return PermissionCheckResult.Granted(manifest.Id, required);
        }

        var missingPermissions = required & ~granted;

        return Policy.Mode switch
        {
            PermissionEnforcementMode.Strict => PermissionCheckResult.Denied(
                manifest.Id,
                required,
                missingPermissions,
                $"插件 {manifest.Id} 缺少必要权限：{missingPermissions}（严格模式）"
            ),
            PermissionEnforcementMode.Permissive => PermissionCheckResult.GrantedWithWarning(
                manifest.Id,
                required,
                missingPermissions,
                $"插件 {manifest.Id} 缺少权限 {missingPermissions}，但宽松模式允许继续执行"
            ),
            _ => PermissionCheckResult.Denied(
                manifest.Id,
                required,
                missingPermissions,
                $"插件 {manifest.Id} 缺少必要权限：{missingPermissions}"
            )
        };
    }

    /// <summary>
    /// 检查插件是否拥有执行原生调用的权限
    /// </summary>
    /// <param name="manifest">插件清单</param>
    /// <returns>检查结果</returns>
    public PermissionCheckResult CheckNativeCall(PluginManifest manifest)
    {
        return Check(manifest, PluginPermission.NativeCall);
    }

    /// <summary>
    /// 检查插件是否拥有网络访问权限
    /// </summary>
    /// <param name="manifest">插件清单</param>
    /// <returns>检查结果</returns>
    public PermissionCheckResult CheckNetworkAccess(PluginManifest manifest)
    {
        return Check(manifest, PluginPermission.NetworkAccess);
    }

    /// <summary>
    /// 检查插件是否拥有文件系统读取权限
    /// </summary>
    /// <param name="manifest">插件清单</param>
    /// <returns>检查结果</returns>
    public PermissionCheckResult CheckFileSystemRead(PluginManifest manifest)
    {
        return Check(manifest, PluginPermission.FileSystemRead);
    }

    /// <summary>
    /// 检查插件是否拥有文件系统写入权限
    /// </summary>
    /// <param name="manifest">插件清单</param>
    /// <returns>检查结果</returns>
    public PermissionCheckResult CheckFileSystemWrite(PluginManifest manifest)
    {
        return Check(manifest, PluginPermission.FileSystemWrite);
    }

    /// <summary>
    /// 注册自定义权限
    /// </summary>
    /// <param name="name">权限名称</param>
    /// <param name="permission">权限标志</param>
    public void RegisterCustomPermission(string name, PluginPermission permission)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        _customPermissions[name] = permission;
    }

    /// <summary>
    /// 查找自定义权限
    /// </summary>
    /// <param name="name">权限名称</param>
    /// <returns>权限标志，未找到返回 null</returns>
    public PluginPermission? FindCustomPermission(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _customPermissions.GetValueOrDefault(name);
    }

    #endregion
}

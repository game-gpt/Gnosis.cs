using Gnosis.Plugin.Host;
using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Isolation;

/// <summary>
/// 插件沙箱隔离环境，限制插件的资源访问与行为边界
/// </summary>
public sealed class PluginSandbox : IDisposable
{
    #region 字段

    private readonly string _pluginId;
    private readonly PluginPermission _grantedPermissions;
    private readonly ResourceQuota _quota;
    private readonly SandboxPolicy _policy;
    private readonly Dictionary<string, object> _isolatedStorage;
    private readonly HashSet<string> _allowedPaths;
    private readonly HashSet<string> _deniedPaths;
    private long _memoryUsage;
    private bool _isDisposed;

    #endregion

    #region 属性

    /// <summary>
    /// 沙箱所属的插件标识
    /// </summary>
    public string PluginId => _pluginId;

    /// <summary>
    /// 沙箱是否已激活
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// 当前内存使用量（字节）
    /// </summary>
    public long MemoryUsage => _memoryUsage;

    /// <summary>
    /// 资源配额
    /// </summary>
    public ResourceQuota Quota => _quota;

    /// <summary>
    /// 沙箱策略
    /// </summary>
    public SandboxPolicy Policy => _policy;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用插件上下文和沙箱策略初始化插件沙箱
    /// </summary>
    /// <param name="context">插件上下文</param>
    /// <param name="policy">沙箱策略，为 null 时使用默认策略</param>
    public PluginSandbox(IPluginContext context, SandboxPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        _pluginId = context.PluginId;
        _grantedPermissions = context.Manifest.Permissions;
        _quota = new ResourceQuota();
        _policy = policy ?? SandboxPolicy.Default;
        _isolatedStorage = new Dictionary<string, object>();
        _allowedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _deniedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _memoryUsage = 0;
        IsActive = true;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 检查插件是否拥有指定权限
    /// </summary>
    /// <param name="permission">待检查的权限</param>
    /// <returns>是否拥有该权限</returns>
    public bool HasPermission(PluginPermission permission)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        return (_grantedPermissions & permission) == permission;
    }

    /// <summary>
    /// 请求指定权限，若未授予则抛出异常
    /// </summary>
    /// <param name="permission">待请求的权限</param>
    /// <exception cref="SandboxViolationException">权限未授予时抛出</exception>
    public void DemandPermission(PluginPermission permission)
    {
        if (!HasPermission(permission))
        {
            throw new SandboxViolationException(
                _pluginId,
                $"插件 {_pluginId} 缺少必要权限：{permission}"
            );
        }
    }

    /// <summary>
    /// 检查文件路径是否可访问
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="access">访问类型</param>
    /// <returns>是否可访问</returns>
    public bool CanAccessPath(string path, FileAccessMode access)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (_deniedPaths.Contains(path))
        {
            return false;
        }

        if (access == FileAccessMode.Read && !HasPermission(PluginPermission.FileSystemRead))
        {
            return false;
        }

        if (access == FileAccessMode.Write && !HasPermission(PluginPermission.FileSystemWrite))
        {
            return false;
        }

        if (_allowedPaths.Count > 0 && !_allowedPaths.Contains(path))
        {
            return _policy.AllowDefaultPathAccess;
        }

        return true;
    }

    /// <summary>
    /// 授权文件路径访问
    /// </summary>
    /// <param name="path">文件路径</param>
    public void AllowPath(string path)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _allowedPaths.Add(path);
        _deniedPaths.Remove(path);
    }

    /// <summary>
    /// 拒绝文件路径访问
    /// </summary>
    /// <param name="path">文件路径</param>
    public void DenyPath(string path)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        _deniedPaths.Add(path);
        _allowedPaths.Remove(path);
    }

    /// <summary>
    /// 分配内存配额
    /// </summary>
    /// <param name="size">请求分配的字节数</param>
    /// <returns>是否分配成功</returns>
    public bool TryAllocateMemory(long size)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        if (size <= 0)
        {
            return false;
        }

        if (!_quota.MemoryQuota.HasValue)
        {
            _memoryUsage += size;
            return true;
        }

        if (_memoryUsage + size > _quota.MemoryQuota.Value)
        {
            return false;
        }

        _memoryUsage += size;
        return true;
    }

    /// <summary>
    /// 释放已分配的内存配额
    /// </summary>
    /// <param name="size">释放的字节数</param>
    public void ReleaseMemory(long size)
    {
        ThrowIfDisposed();

        if (size <= 0)
        {
            return;
        }

        _memoryUsage = Math.Max(0, _memoryUsage - size);
    }

    /// <summary>
    /// 在沙箱隔离存储中设置数据
    /// </summary>
    /// <param name="key">数据键</param>
    /// <param name="value">数据值</param>
    public void SetData(string key, object value)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _isolatedStorage[key] = value;
    }

    /// <summary>
    /// 从沙箱隔离存储中获取数据
    /// </summary>
    /// <param name="key">数据键</param>
    /// <returns>数据值，未找到返回 null</returns>
    public object? GetData(string key)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return _isolatedStorage.GetValueOrDefault(key);
    }

    /// <summary>
    /// 从沙箱隔离存储中获取数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    /// <returns>类型化的数据值</returns>
    public T? GetData<T>(string key)
    {
        var value = GetData(key);

        if (value is T typed)
        {
            return typed;
        }

        return default;
    }

    /// <summary>
    /// 挂起沙箱（暂停插件执行）
    /// </summary>
    public void Suspend()
    {
        ThrowIfDisposed();
        IsActive = false;
    }

    /// <summary>
    /// 恢复沙箱（继续插件执行）
    /// </summary>
    public void Resume()
    {
        ThrowIfDisposed();
        IsActive = true;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 若已释放则抛出异常
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(PluginSandbox));
        }
    }

    /// <summary>
    /// 若未激活则抛出异常
    /// </summary>
    private void ThrowIfNotActive()
    {
        if (!IsActive)
        {
            throw new SandboxViolationException(
                _pluginId,
                $"插件 {_pluginId} 的沙箱已挂起，无法执行操作"
            );
        }
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放沙箱资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isolatedStorage.Clear();
        _allowedPaths.Clear();
        _deniedPaths.Clear();
        _memoryUsage = 0;
        IsActive = false;
        _isDisposed = true;
    }

    #endregion
}

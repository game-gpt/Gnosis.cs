using Gnosis.Plugin.Isolation;
using Gnosis.Plugin.Manifest;

namespace Gnosis.Plugin.Context;

/// <summary>
/// 插件隔离上下文，为每个插件提供独立的运行环境
/// </summary>
public sealed class PluginIsolationContext : IDisposable
{
    #region 字段

    private readonly PluginSandbox _sandbox;
    private readonly Dictionary<string, object> _environmentBindings;
    private readonly HashSet<string> _exportedServices;
    private readonly HashSet<string> _importedServices;
    private bool _isDisposed;

    #endregion

    #region 属性

    /// <summary>
    /// 插件标识
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// 插件清单
    /// </summary>
    public PluginManifest Manifest { get; }

    /// <summary>
    /// 插件沙箱
    /// </summary>
    public PluginSandbox Sandbox => _sandbox;

    /// <summary>
    /// 环境绑定字典
    /// </summary>
    public IReadOnlyDictionary<string, object> EnvironmentBindings => _environmentBindings;

    /// <summary>
    /// 已导出的服务名称集合
    /// </summary>
    public IReadOnlySet<string> ExportedServices => _exportedServices;

    /// <summary>
    /// 已导入的服务名称集合
    /// </summary>
    public IReadOnlySet<string> ImportedServices => _importedServices;

    /// <summary>
    /// 上下文是否已激活
    /// </summary>
    public bool IsActive => !_isDisposed && _sandbox.IsActive;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用插件清单和沙箱策略初始化隔离上下文
    /// </summary>
    /// <param name="manifest">插件清单</param>
    /// <param name="policy">沙箱策略，为 null 时使用默认策略</param>
    public PluginIsolationContext(PluginManifest manifest, SandboxPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        PluginId = manifest.Id;
        Manifest = manifest;
        _sandbox = new PluginSandbox(new Host.PluginContext(manifest.Id, manifest, null!), policy);
        _environmentBindings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        _exportedServices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _importedServices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 绑定环境变量到上下文
    /// </summary>
    /// <param name="key">环境变量键</param>
    /// <param name="value">环境变量值</param>
    public void BindEnvironment(string key, object value)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _environmentBindings[key] = value;
    }

    /// <summary>
    /// 解绑环境变量
    /// </summary>
    /// <param name="key">环境变量键</param>
    /// <returns>是否解绑成功</returns>
    public bool UnbindEnvironment(string key)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return _environmentBindings.Remove(key);
    }

    /// <summary>
    /// 获取绑定的环境变量
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">环境变量键</param>
    /// <returns>绑定的值，未找到返回 default</returns>
    public T? GetEnvironment<T>(string key)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(key))
        {
            return default;
        }

        if (_environmentBindings.TryGetValue(key, out var value) && value is T typed)
        {
            return typed;
        }

        return default;
    }

    /// <summary>
    /// 注册导出服务
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    public void ExportService(string serviceName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return;
        }

        _exportedServices.Add(serviceName);
    }

    /// <summary>
    /// 注册导入服务
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    public void ImportService(string serviceName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return;
        }

        _importedServices.Add(serviceName);
    }

    /// <summary>
    /// 检查是否导出了指定服务
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <returns>是否已导出</returns>
    public bool HasExportedService(string serviceName)
    {
        return _exportedServices.Contains(serviceName);
    }

    /// <summary>
    /// 检查是否导入了指定服务
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <returns>是否已导入</returns>
    public bool HasImportedService(string serviceName)
    {
        return _importedServices.Contains(serviceName);
    }

    /// <summary>
    /// 检查插件是否拥有指定权限
    /// </summary>
    /// <param name="permission">待检查的权限</param>
    /// <returns>是否拥有该权限</returns>
    public bool HasPermission(PluginPermission permission)
    {
        return _sandbox.HasPermission(permission);
    }

    /// <summary>
    /// 请求指定权限，若未授予则抛出异常
    /// </summary>
    /// <param name="permission">待请求的权限</param>
    public void DemandPermission(PluginPermission permission)
    {
        _sandbox.DemandPermission(permission);
    }

    /// <summary>
    /// 挂起上下文
    /// </summary>
    public void Suspend()
    {
        ThrowIfDisposed();
        _sandbox.Suspend();
    }

    /// <summary>
    /// 恢复上下文
    /// </summary>
    public void Resume()
    {
        ThrowIfDisposed();
        _sandbox.Resume();
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
            throw new ObjectDisposedException(nameof(PluginIsolationContext));
        }
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放上下文资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _sandbox.Dispose();
        _environmentBindings.Clear();
        _exportedServices.Clear();
        _importedServices.Clear();
        _isDisposed = true;
    }

    #endregion
}

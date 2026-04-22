namespace Gnosis.Plugin.Extension;

/// <summary>
/// 扩展点注册表，管理扩展点的注册、查询与扩展绑定
/// </summary>
public sealed class ExtensionRegistry
{
    #region 字段

    private readonly Dictionary<string, IExtensionPoint> _extensionPoints;
    private readonly Dictionary<string, List<ExtensionRegistration>> _registrations;
    private readonly Dictionary<string, ExtensionRegistration> _registrationsById;

    #endregion

    #region 事件

    /// <summary>
    /// 扩展点注册事件
    /// </summary>
    public event EventHandler<ExtensionPointEventArgs>? ExtensionPointRegistered;

    /// <summary>
    /// 扩展点注销事件
    /// </summary>
    public event EventHandler<ExtensionPointEventArgs>? ExtensionPointUnregistered;

    /// <summary>
    /// 扩展注册事件
    /// </summary>
    public event EventHandler<ExtensionRegistrationEventArgs>? ExtensionRegistered;

    /// <summary>
    /// 扩展注销事件
    /// </summary>
    public event EventHandler<ExtensionRegistrationEventArgs>? ExtensionUnregistered;

    #endregion

    #region 属性

    /// <summary>
    /// 已注册的扩展点数量
    /// </summary>
    public int ExtensionPointCount => _extensionPoints.Count;

    /// <summary>
    /// 已注册的扩展数量
    /// </summary>
    public int RegistrationCount => _registrationsById.Count;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化扩展点注册表
    /// </summary>
    public ExtensionRegistry()
    {
        _extensionPoints = new Dictionary<string, IExtensionPoint>(StringComparer.OrdinalIgnoreCase);
        _registrations = new Dictionary<string, List<ExtensionRegistration>>(StringComparer.OrdinalIgnoreCase);
        _registrationsById = new Dictionary<string, ExtensionRegistration>(StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region 扩展点管理

    /// <summary>
    /// 注册扩展点
    /// </summary>
    /// <param name="extensionPoint">扩展点</param>
    public void RegisterExtensionPoint(IExtensionPoint extensionPoint)
    {
        ArgumentNullException.ThrowIfNull(extensionPoint);

        if (_extensionPoints.ContainsKey(extensionPoint.Id))
        {
            throw new InvalidOperationException($"扩展点 {extensionPoint.Id} 已注册");
        }

        _extensionPoints[extensionPoint.Id] = extensionPoint;
        _registrations[extensionPoint.Id] = new List<ExtensionRegistration>();

        OnExtensionPointRegistered(new ExtensionPointEventArgs(extensionPoint));
    }

    /// <summary>
    /// 注销扩展点
    /// </summary>
    /// <param name="extensionPointId">扩展点标识</param>
    /// <returns>是否注销成功</returns>
    public bool UnregisterExtensionPoint(string extensionPointId)
    {
        if (string.IsNullOrWhiteSpace(extensionPointId))
        {
            return false;
        }

        if (!_extensionPoints.Remove(extensionPointId, out var point))
        {
            return false;
        }

        if (_registrations.TryGetValue(extensionPointId, out var regs))
        {
            foreach (var reg in regs)
            {
                _registrationsById.Remove(reg.Id);
            }

            _registrations.Remove(extensionPointId);
        }

        OnExtensionPointUnregistered(new ExtensionPointEventArgs(point));

        return true;
    }

    /// <summary>
    /// 注销指定插件声明的所有扩展点
    /// </summary>
    /// <param name="ownerPluginId">插件标识</param>
    /// <returns>注销的扩展点数量</returns>
    public int UnregisterExtensionPointsByOwner(string ownerPluginId)
    {
        var toRemove = _extensionPoints.Values
            .Where(ep => string.Equals(ep.OwnerPluginId, ownerPluginId, StringComparison.OrdinalIgnoreCase))
            .Select(ep => ep.Id)
            .ToList();

        foreach (var extensionPointId in toRemove)
        {
            UnregisterExtensionPoint(extensionPointId);
        }

        return toRemove.Count;
    }

    /// <summary>
    /// 查询扩展点
    /// </summary>
    /// <param name="extensionPointId">扩展点标识</param>
    /// <returns>扩展点，未找到返回 null</returns>
    public IExtensionPoint? GetExtensionPoint(string extensionPointId)
    {
        if (string.IsNullOrWhiteSpace(extensionPointId))
        {
            return null;
        }

        return _extensionPoints.GetValueOrDefault(extensionPointId);
    }

    /// <summary>
    /// 查询所有扩展点
    /// </summary>
    /// <returns>扩展点集合</returns>
    public IReadOnlyCollection<IExtensionPoint> GetAllExtensionPoints()
    {
        return _extensionPoints.Values;
    }

    /// <summary>
    /// 查询指定插件声明的扩展点
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    /// <returns>扩展点集合</returns>
    public IReadOnlyList<IExtensionPoint> GetExtensionPointsByOwner(string pluginId)
    {
        return _extensionPoints.Values
            .Where(ep => string.Equals(ep.OwnerPluginId, pluginId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// 检查扩展点是否已注册
    /// </summary>
    /// <param name="extensionPointId">扩展点标识</param>
    /// <returns>是否已注册</returns>
    public bool HasExtensionPoint(string extensionPointId)
    {
        return !string.IsNullOrWhiteSpace(extensionPointId) && _extensionPoints.ContainsKey(extensionPointId);
    }

    #endregion

    #region 扩展注册管理

    /// <summary>
    /// 注册扩展实现
    /// </summary>
    /// <param name="registration">扩展注册</param>
    public void RegisterExtension(ExtensionRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        if (!_extensionPoints.ContainsKey(registration.ExtensionPointId))
        {
            throw new InvalidOperationException($"扩展点 {registration.ExtensionPointId} 未注册，无法注册扩展");
        }

        var extensionPoint = _extensionPoints[registration.ExtensionPointId];

        if (!extensionPoint.AllowMultiple && _registrations[registration.ExtensionPointId].Count > 0)
        {
            throw new InvalidOperationException($"扩展点 {registration.ExtensionPointId} 不允许多个扩展");
        }

        if (!extensionPoint.ContractType.IsAssignableFrom(registration.ImplementationType))
        {
            throw new InvalidOperationException(
                $"扩展实现类型 {registration.ImplementationType.FullName} 未实现契约类型 {extensionPoint.ContractType.FullName}"
            );
        }

        _registrations[registration.ExtensionPointId].Add(registration);
        _registrationsById[registration.Id] = registration;

        OnExtensionRegistered(new ExtensionRegistrationEventArgs(registration));
    }

    /// <summary>
    /// 注销扩展实现
    /// </summary>
    /// <param name="registrationId">注册标识</param>
    /// <returns>是否注销成功</returns>
    public bool UnregisterExtension(string registrationId)
    {
        if (string.IsNullOrWhiteSpace(registrationId))
        {
            return false;
        }

        if (!_registrationsById.Remove(registrationId, out var registration))
        {
            return false;
        }

        if (_registrations.TryGetValue(registration.ExtensionPointId, out var regs))
        {
            regs.Remove(registration);
        }

        OnExtensionUnregistered(new ExtensionRegistrationEventArgs(registration));

        return true;
    }

    /// <summary>
    /// 注销指定插件的所有扩展注册
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    /// <returns>注销的扩展数量</returns>
    public int UnregisterExtensionsByPlugin(string pluginId)
    {
        var toRemove = _registrationsById.Values
            .Where(r => string.Equals(r.PluginId, pluginId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var registration in toRemove)
        {
            _registrationsById.Remove(registration.Id);

            if (_registrations.TryGetValue(registration.ExtensionPointId, out var regs))
            {
                regs.Remove(registration);
            }
        }

        return toRemove.Count;
    }

    /// <summary>
    /// 获取指定扩展点的所有扩展注册
    /// </summary>
    /// <param name="extensionPointId">扩展点标识</param>
    /// <returns>扩展注册列表（按优先级降序排列）</returns>
    public IReadOnlyList<ExtensionRegistration> GetExtensions(string extensionPointId)
    {
        if (string.IsNullOrWhiteSpace(extensionPointId))
        {
            return Array.Empty<ExtensionRegistration>();
        }

        if (!_registrations.TryGetValue(extensionPointId, out var regs))
        {
            return Array.Empty<ExtensionRegistration>();
        }

        return regs
            .Where(r => r.IsEnabled)
            .OrderByDescending(r => r.Priority)
            .ToList();
    }

    /// <summary>
    /// 获取指定扩展点的最高优先级扩展
    /// </summary>
    /// <param name="extensionPointId">扩展点标识</param>
    /// <returns>最高优先级的扩展注册，未找到返回 null</returns>
    public ExtensionRegistration? GetHighestPriorityExtension(string extensionPointId)
    {
        var extensions = GetExtensions(extensionPointId);
        return extensions.Count > 0 ? extensions[0] : null;
    }

    /// <summary>
    /// 获取指定插件的所有扩展注册
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    /// <returns>扩展注册列表</returns>
    public IReadOnlyList<ExtensionRegistration> GetExtensionsByPlugin(string pluginId)
    {
        return _registrationsById.Values
            .Where(r => string.Equals(r.PluginId, pluginId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// 创建指定扩展点的最高优先级扩展实例
    /// </summary>
    /// <typeparam name="T">契约类型</typeparam>
    /// <param name="extensionPointId">扩展点标识</param>
    /// <returns>扩展实例，未找到返回 null</returns>
    public T? CreateExtension<T>(string extensionPointId) where T : class
    {
        var reg = GetHighestPriorityExtension(extensionPointId);
        return reg?.CreateInstance<T>();
    }

    /// <summary>
    /// 创建指定扩展点的所有扩展实例
    /// </summary>
    /// <typeparam name="T">契约类型</typeparam>
    /// <param name="extensionPointId">扩展点标识</param>
    /// <returns>扩展实例列表</returns>
    public IReadOnlyList<T> CreateAllExtensions<T>(string extensionPointId) where T : class
    {
        return GetExtensions(extensionPointId)
            .Select(r => r.CreateInstance<T>())
            .ToList();
    }

    #endregion

    #region 事件触发

    /// <summary>
    /// 触发扩展点注册事件
    /// </summary>
    private void OnExtensionPointRegistered(ExtensionPointEventArgs e)
    {
        ExtensionPointRegistered?.Invoke(this, e);
    }

    /// <summary>
    /// 触发扩展点注销事件
    /// </summary>
    private void OnExtensionPointUnregistered(ExtensionPointEventArgs e)
    {
        ExtensionPointUnregistered?.Invoke(this, e);
    }

    /// <summary>
    /// 触发扩展注册事件
    /// </summary>
    private void OnExtensionRegistered(ExtensionRegistrationEventArgs e)
    {
        ExtensionRegistered?.Invoke(this, e);
    }

    /// <summary>
    /// 触发扩展注销事件
    /// </summary>
    private void OnExtensionUnregistered(ExtensionRegistrationEventArgs e)
    {
        ExtensionUnregistered?.Invoke(this, e);
    }

    #endregion
}

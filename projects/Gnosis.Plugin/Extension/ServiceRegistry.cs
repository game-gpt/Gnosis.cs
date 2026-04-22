namespace Gnosis.Plugin.Extension;

/// <summary>
/// 插件服务注册表，实现插件间的服务注册与发现
/// </summary>
public sealed class ServiceRegistry
{
    #region 字段

    private readonly Dictionary<Type, List<ServiceDescriptor>> _services;
    private readonly Dictionary<Type, object> _singletonInstances;

    #endregion

    #region 事件

    /// <summary>
    /// 服务注册事件
    /// </summary>
    public event EventHandler<ServiceRegistryEventArgs>? ServiceRegistered;

    /// <summary>
    /// 服务注销事件
    /// </summary>
    public event EventHandler<ServiceRegistryEventArgs>? ServiceUnregistered;

    #endregion

    #region 属性

    /// <summary>
    /// 已注册的服务类型数量
    /// </summary>
    public int ServiceTypeCount => _services.Count;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化服务注册表
    /// </summary>
    public ServiceRegistry()
    {
        _services = new Dictionary<Type, List<ServiceDescriptor>>();
        _singletonInstances = new Dictionary<Type, object>();
    }

    #endregion

    #region 注册

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="descriptor">服务描述</param>
    public void Register(ServiceDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!_services.TryGetValue(descriptor.ServiceType, out var descriptors))
        {
            descriptors = new List<ServiceDescriptor>();
            _services[descriptor.ServiceType] = descriptors;
        }

        descriptors.Add(descriptor);
        descriptors.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        OnServiceRegistered(new ServiceRegistryEventArgs(descriptor));
    }

    /// <summary>
    /// 注册服务（使用工厂方法）
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <param name="pluginId">提供该服务的插件标识</param>
    /// <param name="factory">服务实例工厂</param>
    /// <param name="priority">优先级</param>
    /// <param name="isSingleton">是否为单例</param>
    public void Register<TService>(
        string pluginId,
        Func<TService> factory,
        int priority = 0,
        bool isSingleton = false) where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        var descriptor = new ServiceDescriptor(
            typeof(TService),
            pluginId,
            factory,
            priority,
            isSingleton
        );

        Register(descriptor);
    }

    /// <summary>
    /// 注册单例服务
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <param name="pluginId">提供该服务的插件标识</param>
    /// <param name="instance">服务实例</param>
    /// <param name="priority">优先级</param>
    public void RegisterSingleton<TService>(
        string pluginId,
        TService instance,
        int priority = 0) where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);

        var descriptor = new ServiceDescriptor(
            typeof(TService),
            pluginId,
            () => instance,
            priority,
            true
        );

        _singletonInstances[typeof(TService)] = instance;
        Register(descriptor);
    }

    #endregion

    #region 注销

    /// <summary>
    /// 注销指定插件的所有服务
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    /// <returns>注销的服务数量</returns>
    public int UnregisterByPlugin(string pluginId)
    {
        var count = 0;

        foreach (var (serviceType, descriptors) in _services)
        {
            var toRemove = descriptors
                .Where(d => string.Equals(d.PluginId, pluginId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var descriptor in toRemove)
            {
                descriptors.Remove(descriptor);
                count++;

                if (descriptor.IsSingleton)
                {
                    _singletonInstances.Remove(descriptor.ServiceType);
                }

                OnServiceUnregistered(new ServiceRegistryEventArgs(descriptor));
            }
        }

        var emptyTypes = _services
            .Where(kvp => kvp.Value.Count == 0)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var type in emptyTypes)
        {
            _services.Remove(type);
        }

        return count;
    }

    /// <summary>
    /// 注销指定类型的服务
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>注销的服务数量</returns>
    public int Unregister<TService>() where TService : class
    {
        var serviceType = typeof(TService);

        if (!_services.TryGetValue(serviceType, out var descriptors))
        {
            return 0;
        }

        var count = descriptors.Count;
        _services.Remove(serviceType);
        _singletonInstances.Remove(serviceType);

        return count;
    }

    #endregion

    #region 查询

    /// <summary>
    /// 获取指定服务类型的最高优先级服务实例
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>服务实例，未找到返回 null</returns>
    public TService? GetService<TService>() where TService : class
    {
        var serviceType = typeof(TService);

        if (!_services.TryGetValue(serviceType, out var descriptors) || descriptors.Count == 0)
        {
            return null;
        }

        var descriptor = descriptors[0];

        if (descriptor.IsSingleton && _singletonInstances.TryGetValue(serviceType, out var instance))
        {
            return (TService)instance;
        }

        var created = (TService)descriptor.Factory();

        if (descriptor.IsSingleton)
        {
            _singletonInstances[serviceType] = created;
        }

        return created;
    }

    /// <summary>
    /// 获取指定服务类型的所有服务实例
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>服务实例列表</returns>
    public IReadOnlyList<TService> GetAllServices<TService>() where TService : class
    {
        var serviceType = typeof(TService);

        if (!_services.TryGetValue(serviceType, out var descriptors))
        {
            return Array.Empty<TService>();
        }

        return descriptors
            .Select(d =>
            {
                if (d.IsSingleton && _singletonInstances.TryGetValue(d.ServiceType, out var instance))
                {
                    return (TService)instance;
                }

                var created = (TService)d.Factory();

                if (d.IsSingleton)
                {
                    _singletonInstances[d.ServiceType] = created;
                }

                return created;
            })
            .ToList();
    }

    /// <summary>
    /// 检查指定服务类型是否已注册
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>是否已注册</returns>
    public bool IsRegistered<TService>() where TService : class
    {
        return _services.ContainsKey(typeof(TService)) && _services[typeof(TService)].Count > 0;
    }

    /// <summary>
    /// 获取指定服务类型的所有服务描述
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>服务描述列表</returns>
    public IReadOnlyList<ServiceDescriptor> GetServiceDescriptors<TService>() where TService : class
    {
        if (!_services.TryGetValue(typeof(TService), out var descriptors))
        {
            return Array.Empty<ServiceDescriptor>();
        }

        return descriptors;
    }

    /// <summary>
    /// 按标签查询服务
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <param name="tag">服务标签</param>
    /// <returns>匹配的服务实例列表</returns>
    public IReadOnlyList<TService> GetServicesByTag<TService>(string tag) where TService : class
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return Array.Empty<TService>();
        }

        var serviceType = typeof(TService);

        if (!_services.TryGetValue(serviceType, out var descriptors))
        {
            return Array.Empty<TService>();
        }

        return descriptors
            .Where(d => d.Tags.Contains(tag))
            .Select(d => (TService)d.Factory())
            .ToList();
    }

    /// <summary>
    /// 获取指定插件提供的所有服务描述
    /// </summary>
    /// <param name="pluginId">插件标识</param>
    /// <returns>服务描述列表</returns>
    public IReadOnlyList<ServiceDescriptor> GetServicesByPlugin(string pluginId)
    {
        return _services.Values
            .SelectMany(descriptors => descriptors)
            .Where(d => string.Equals(d.PluginId, pluginId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    #endregion

    #region 事件触发

    /// <summary>
    /// 触发服务注册事件
    /// </summary>
    private void OnServiceRegistered(ServiceRegistryEventArgs e)
    {
        ServiceRegistered?.Invoke(this, e);
    }

    /// <summary>
    /// 触发服务注销事件
    /// </summary>
    private void OnServiceUnregistered(ServiceRegistryEventArgs e)
    {
        ServiceUnregistered?.Invoke(this, e);
    }

    #endregion
}

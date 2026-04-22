namespace Gnosis.Plugin.Extension;

/// <summary>
/// 服务描述信息，记录插件注册的服务元数据
/// </summary>
public sealed class ServiceDescriptor
{
    #region 属性

    /// <summary>
    /// 服务类型
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// 提供该服务的插件标识
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// 服务实例工厂
    /// </summary>
    public Func<object> Factory { get; }

    /// <summary>
    /// 服务优先级（数值越大优先级越高）
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// 服务是否为单例
    /// </summary>
    public bool IsSingleton { get; }

    /// <summary>
    /// 服务标签（用于分类过滤）
    /// </summary>
    public IReadOnlySet<string> Tags { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化服务描述
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="pluginId">提供该服务的插件标识</param>
    /// <param name="factory">服务实例工厂</param>
    /// <param name="priority">优先级</param>
    /// <param name="isSingleton">是否为单例</param>
    /// <param name="tags">服务标签</param>
    public ServiceDescriptor(
        Type serviceType,
        string pluginId,
        Func<object> factory,
        int priority = 0,
        bool isSingleton = false,
        IReadOnlySet<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(factory);

        if (string.IsNullOrWhiteSpace(pluginId))
        {
            throw new ArgumentException("插件标识不能为空", nameof(pluginId));
        }

        ServiceType = serviceType;
        PluginId = pluginId;
        Factory = factory;
        Priority = priority;
        IsSingleton = isSingleton;
        Tags = tags ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    #endregion
}

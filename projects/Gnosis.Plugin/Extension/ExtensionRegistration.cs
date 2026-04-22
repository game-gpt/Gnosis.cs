namespace Gnosis.Plugin.Extension;

/// <summary>
/// 扩展点注册，描述一个插件对某个扩展点的具体扩展实现
/// </summary>
public sealed class ExtensionRegistration
{
    #region 属性

    /// <summary>
    /// 注册标识
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 扩展点标识
    /// </summary>
    public string ExtensionPointId { get; }

    /// <summary>
    /// 提供扩展的插件标识
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// 扩展实现类型
    /// </summary>
    public Type ImplementationType { get; }

    /// <summary>
    /// 扩展优先级（数值越大优先级越高）
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// 是否已启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 扩展元数据
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化扩展注册
    /// </summary>
    public ExtensionRegistration(
        string id,
        string extensionPointId,
        string pluginId,
        Type implementationType,
        int priority = 0,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Id = id;
        ExtensionPointId = extensionPointId;
        PluginId = pluginId;
        ImplementationType = implementationType;
        Priority = priority;
        IsEnabled = true;
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 创建扩展实现实例
    /// </summary>
    /// <returns>扩展实现实例</returns>
    public object CreateInstance()
    {
        return Activator.CreateInstance(ImplementationType)!
            ?? throw new InvalidOperationException($"无法创建类型 {ImplementationType.FullName} 的实例");
    }

    /// <summary>
    /// 创建类型化的扩展实现实例
    /// </summary>
    /// <typeparam name="T">契约类型</typeparam>
    /// <returns>类型化的扩展实现实例</returns>
    public T CreateInstance<T>() where T : class
    {
        return (T)CreateInstance();
    }

    #endregion
}

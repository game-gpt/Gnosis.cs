namespace Gnosis.Toolchain.ScriptCompiler;

/// <summary>
/// 能力注册表的密封实现，管理插件与能力的映射关系
/// </summary>
public sealed class CapabilityRegistry : ICapabilityRegistry
{
    #region Fields

    private readonly Dictionary<string, string> _capabilities = new(StringComparer.Ordinal);

    #endregion

    #region Public Methods

    /// <summary>
    /// 注册一个由插件提供的能力，后注册的会覆盖先注册的
    /// </summary>
    /// <param name="capabilityName">能力名称</param>
    /// <param name="pluginName">提供该能力的插件名称</param>
    public void Register(string capabilityName, string pluginName)
    {
        _capabilities[capabilityName] = pluginName;
    }

    /// <summary>
    /// 获取提供指定能力的插件名称
    /// </summary>
    /// <param name="capabilityName">能力名称</param>
    /// <returns>提供该能力的插件名称，未找到则返回 null</returns>
    public string? GetProvider(string capabilityName)
    {
        return _capabilities.GetValueOrDefault(capabilityName);
    }

    /// <summary>
    /// 检查指定能力是否已注册
    /// </summary>
    /// <param name="capabilityName">能力名称</param>
    /// <returns>已注册返回 true，否则返回 false</returns>
    public bool Contains(string capabilityName)
    {
        return _capabilities.ContainsKey(capabilityName);
    }

    /// <summary>
    /// 获取所有已注册的能力及其提供者
    /// </summary>
    /// <returns>能力名称到插件名称的只读字典</returns>
    public IReadOnlyDictionary<string, string> GetAll()
    {
        return _capabilities;
    }

    #endregion
}

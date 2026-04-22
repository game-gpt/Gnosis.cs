namespace Gnosis.Toolchain.ScriptCompiler;

/// <summary>
/// 能力注册表接口，用于管理插件提供的能力
/// </summary>
public interface ICapabilityRegistry
{
    /// <summary>
    /// 注册一个由插件提供的能力
    /// </summary>
    /// <param name="capabilityName">能力名称</param>
    /// <param name="pluginName">提供该能力的插件名称</param>
    void Register(string capabilityName, string pluginName);

    /// <summary>
    /// 获取提供指定能力的插件名称
    /// </summary>
    /// <param name="capabilityName">能力名称</param>
    /// <returns>提供该能力的插件名称，未找到则返回 null</returns>
    string? GetProvider(string capabilityName);

    /// <summary>
    /// 检查指定能力是否已注册
    /// </summary>
    /// <param name="capabilityName">能力名称</param>
    /// <returns>已注册返回 true，否则返回 false</returns>
    bool Contains(string capabilityName);

    /// <summary>
    /// 获取所有已注册的能力及其提供者
    /// </summary>
    /// <returns>能力名称到插件名称的只读字典</returns>
    IReadOnlyDictionary<string, string> GetAll();
}

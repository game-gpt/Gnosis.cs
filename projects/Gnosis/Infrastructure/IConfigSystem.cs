using Gnosis.Infrastructure.Events;

namespace Gnosis.Infrastructure;

/// <summary>
/// 配置系统接口，负责加载、读取和运行时修改配置
/// </summary>
public interface IConfigSystem
{
    #region Properties

    /// <summary>
    /// 配置变更事件回调
    /// </summary>
    Action<ConfigChangedEvent>? OnConfigChanged { get; set; }

    #endregion

    #region Methods

    /// <summary>
    /// 从指定路径加载配置文件
    /// </summary>
    void Load(string path);

    /// <summary>
    /// 获取指定键的配置值并转换为目标类型
    /// </summary>
    T Get<T>(string key);

    /// <summary>
    /// 获取指定键的配置值，键不存在时返回默认值
    /// </summary>
    T Get<T>(string key, T defaultValue);

    /// <summary>
    /// 设置配置键值，并触发 ConfigChangedEvent
    /// </summary>
    void Set(string key, string value);

    /// <summary>
    /// 检查指定配置键是否存在
    /// </summary>
    bool ContainsKey(string key);

    /// <summary>
    /// 获取指定配置键的来源文件路径
    /// </summary>
    string? GetSource(string key);

    #endregion
}

namespace Gnosis.Plugin.Extension;

/// <summary>
/// 扩展点接口，定义插件可扩展的契约
/// </summary>
public interface IExtensionPoint
{
    /// <summary>
    /// 扩展点唯一标识
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 扩展点名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 扩展点描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 扩展点契约类型
    /// </summary>
    Type ContractType { get; }

    /// <summary>
    /// 声明该扩展点的插件标识
    /// </summary>
    string OwnerPluginId { get; }

    /// <summary>
    /// 是否允许多个扩展
    /// </summary>
    bool AllowMultiple { get; }
}

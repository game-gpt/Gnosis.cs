namespace Gnosis.Plugin.Extension;

/// <summary>
/// 扩展点实现，描述引擎或插件声明的可扩展位置
/// </summary>
public sealed class ExtensionPoint : IExtensionPoint
{
    #region 属性

    /// <summary>
    /// 扩展点唯一标识
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 扩展点名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 扩展点描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 扩展点契约类型
    /// </summary>
    public Type ContractType { get; }

    /// <summary>
    /// 声明该扩展点的插件标识
    /// </summary>
    public string OwnerPluginId { get; }

    /// <summary>
    /// 是否允许多个扩展
    /// </summary>
    public bool AllowMultiple { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化扩展点
    /// </summary>
    /// <param name="id">扩展点唯一标识</param>
    /// <param name="name">扩展点名称</param>
    /// <param name="description">扩展点描述</param>
    /// <param name="contractType">契约类型</param>
    /// <param name="ownerPluginId">声明该扩展点的插件标识</param>
    /// <param name="allowMultiple">是否允许多个扩展</param>
    public ExtensionPoint(
        string id,
        string name,
        string description,
        Type contractType,
        string ownerPluginId,
        bool allowMultiple = true)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("扩展点标识不能为空", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("扩展点名称不能为空", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(contractType);

        Id = id;
        Name = name;
        Description = description ?? string.Empty;
        ContractType = contractType;
        OwnerPluginId = ownerPluginId;
        AllowMultiple = allowMultiple;
    }

    #endregion
}

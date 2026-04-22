namespace Gnosis.Plugin.Extension;

/// <summary>
/// 服务注册表事件参数
/// </summary>
public class ServiceRegistryEventArgs : EventArgs
{
    #region 属性

    /// <summary>
    /// 服务描述
    /// </summary>
    public ServiceDescriptor Descriptor { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化服务注册表事件参数
    /// </summary>
    /// <param name="descriptor">服务描述</param>
    public ServiceRegistryEventArgs(ServiceDescriptor descriptor)
    {
        Descriptor = descriptor;
    }

    #endregion
}

namespace Gnosis.Plugin.Extension;

/// <summary>
/// 扩展注册事件参数
/// </summary>
public class ExtensionRegistrationEventArgs : EventArgs
{
    #region 属性

    /// <summary>
    /// 扩展注册
    /// </summary>
    public ExtensionRegistration Registration { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化扩展注册事件参数
    /// </summary>
    /// <param name="registration">扩展注册</param>
    public ExtensionRegistrationEventArgs(ExtensionRegistration registration)
    {
        Registration = registration;
    }

    #endregion
}

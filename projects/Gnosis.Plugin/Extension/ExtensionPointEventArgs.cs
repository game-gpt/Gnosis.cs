namespace Gnosis.Plugin.Extension;

/// <summary>
/// 扩展点事件参数
/// </summary>
public class ExtensionPointEventArgs : EventArgs
{
    #region 属性

    /// <summary>
    /// 扩展点
    /// </summary>
    public IExtensionPoint ExtensionPoint { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化扩展点事件参数
    /// </summary>
    /// <param name="extensionPoint">扩展点</param>
    public ExtensionPointEventArgs(IExtensionPoint extensionPoint)
    {
        ExtensionPoint = extensionPoint;
    }

    #endregion
}

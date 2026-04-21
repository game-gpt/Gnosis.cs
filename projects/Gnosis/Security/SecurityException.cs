namespace Gnosis.Security;

/// <summary>
/// 安全异常，当安全检查失败时抛出。
/// </summary>
public class SecurityException : Exception
{
    /// <summary>
    /// 初始化 <see cref="SecurityException"/> 类的新实例，使用默认错误消息。
    /// </summary>
    public SecurityException()
        : base("安全检查失败")
    {
    }

    /// <summary>
    /// 初始化 <see cref="SecurityException"/> 类的新实例，使用指定的错误消息。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    public SecurityException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 初始化 <see cref="SecurityException"/> 类的新实例，使用指定的错误消息和内部异常。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    /// <param name="innerException">导致当前异常的内部异常。</param>
    public SecurityException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

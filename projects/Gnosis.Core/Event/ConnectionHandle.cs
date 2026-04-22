namespace Gnosis.Core.Event;

/// <summary>
/// 信号连接句柄，用于断开信号与处理器的连接
/// </summary>
public readonly record struct ConnectionHandle
{
    /// <summary>
    /// 连接的唯一标识符
    /// </summary>
    internal int Id { get; init; }

    /// <summary>
    /// 连接在处理器列表中的索引
    /// </summary>
    internal int Index { get; init; }
}

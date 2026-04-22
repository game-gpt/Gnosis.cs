namespace Gnosis.Core.Event;

/// <summary>
/// 事件总线订阅令牌，用于取消订阅
/// </summary>
public readonly record struct SubscriptionToken
{
    /// <summary>
    /// 订阅的唯一标识符
    /// </summary>
    internal int Id { get; init; }
}

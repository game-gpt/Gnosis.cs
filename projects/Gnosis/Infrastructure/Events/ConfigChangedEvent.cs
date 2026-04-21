using Gnosis.ECS.Core;

namespace Gnosis.Infrastructure.Events;

/// <summary>
/// 配置变更事件，当配置键值被运行时修改时触发
/// </summary>
public record ConfigChangedEvent : DomainEventBase
{
    /// <summary>
    /// 发生变更的配置键
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// 变更前的值，键为新增时为 null
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// 变更后的值，键被删除时为 null
    /// </summary>
    public string? NewValue { get; }

    public ConfigChangedEvent(EntityId aggregateId, string key, string? oldValue, string? newValue)
        : base(aggregateId)
    {
        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

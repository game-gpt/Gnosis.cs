using Gnosis.ECS.Core;

namespace Gnosis.Infrastructure.Events;

public interface IDomainEvent
{
    EntityId AggregateId { get; }
    Timestamp OccurredOn { get; }
}

using Gnosis.ECS.Core;

namespace Gnosis.Infrastructure.Events;

public abstract record DomainEventBase(EntityId AggregateId) : IDomainEvent
{
    public Timestamp OccurredOn { get; } = Timestamp.Now;
}

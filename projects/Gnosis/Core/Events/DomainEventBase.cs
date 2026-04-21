using Gnosis.ECS.Core;
using Gnosis.Infrastructure;

namespace Gnosis.Core.Events;

public abstract record DomainEventBase(EntityId AggregateId) : IDomainEvent
{
    public Timestamp OccurredOn { get; } = Timestamp.Now;
}

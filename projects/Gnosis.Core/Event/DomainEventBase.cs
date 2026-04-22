using Gnosis.Core;
using Gnosis.Core.Time;

namespace Gnosis.Core.Event;

public abstract record DomainEventBase(EntityId AggregateId) : IDomainEvent
{
    public Timestamp OccurredOn { get; } = Timestamp.Now;
}

using Gnosis.Core.Time;

namespace Gnosis.Core.Event;

public interface IDomainEvent
{
    EntityId AggregateId { get; }
    Timestamp OccurredOn { get; }
}

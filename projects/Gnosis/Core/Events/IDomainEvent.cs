using Gnosis.Core;

namespace Gnosis.Core.Events;

public interface IDomainEvent
{
    EntityId AggregateId { get; }
    Timestamp OccurredOn { get; }
}

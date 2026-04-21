using Gnosis.ECS.Core;
using Gnosis.Infrastructure;

namespace Gnosis.Core.Events;

public interface IDomainEvent
{
    EntityId AggregateId { get; }
    Timestamp OccurredOn { get; }
}

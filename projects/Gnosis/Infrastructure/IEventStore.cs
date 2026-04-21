using Gnosis.Core;
using Gnosis.Core.Events;

namespace Gnosis.Infrastructure;

public interface IEventStore
{
    Task AppendAsync(EntityId aggregateId, IDomainEvent @event);
    Task<IEnumerable<IDomainEvent>> GetEventsAsync(EntityId aggregateId);
    Task<IEnumerable<IDomainEvent>> GetEventsSinceAsync(Timestamp timestamp);
    Task SaveChangesAsync();
}

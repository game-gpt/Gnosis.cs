using Gnosis.Core.Event;
using Gnosis.Core.Time;

namespace Gnosis.Core.Event;

public interface IEventStore
{
    Task AppendAsync(EntityId aggregateId, IDomainEvent @event);
    Task<IEnumerable<IDomainEvent>> GetEventsAsync(EntityId aggregateId);
    Task<IEnumerable<IDomainEvent>> GetEventsSinceAsync(Timestamp timestamp);
    Task SaveChangesAsync();
}

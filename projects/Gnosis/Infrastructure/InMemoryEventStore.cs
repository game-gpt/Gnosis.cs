using Gnosis.Core;
using Gnosis.Core.Events;

namespace Gnosis.Infrastructure;

public class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<EntityId, List<IDomainEvent>> _events = new();
    
    public Task AppendAsync(EntityId aggregateId, IDomainEvent @event)
    {
        if (!_events.ContainsKey(aggregateId))
        {
            _events[aggregateId] = new List<IDomainEvent>();
        }
        _events[aggregateId].Add(@event);
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<IDomainEvent>> GetEventsAsync(EntityId aggregateId)
    {
        if (_events.TryGetValue(aggregateId, out var events))
        {
            return Task.FromResult(events.AsEnumerable());
        }
        return Task.FromResult(Enumerable.Empty<IDomainEvent>());
    }
    
    public Task<IEnumerable<IDomainEvent>> GetEventsSinceAsync(Timestamp timestamp)
    {
        var allEvents = _events.Values
            .SelectMany(e => e)
            .Where(e => e.OccurredOn >= timestamp);
        return Task.FromResult(allEvents);
    }
    
    public Task SaveChangesAsync()
    {
        return Task.CompletedTask;
    }
}

using Gnosis.Core.ValueObjects;

namespace Gnosis.ECS.Interfaces;

public interface IComponentPool
{
    void Add<T>(EntityId entityId, T component) where T : struct;
    T Get<T>(EntityId entityId) where T : struct;
    void Remove<T>(EntityId entityId) where T : struct;
    bool Has<T>(EntityId entityId) where T : struct;
    
    int Count { get; }
}

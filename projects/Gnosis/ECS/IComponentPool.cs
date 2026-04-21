using Gnosis.Core;

namespace Gnosis.ECS;

public interface IComponentPool
{
    Type ComponentType { get; }
    void Add<T>(EntityId entityId, T component) where T : struct;
    T Get<T>(EntityId entityId) where T : struct;
    void Remove<T>(EntityId entityId) where T : struct;
    bool Has<T>(EntityId entityId) where T : struct;

    int Count { get; }

    IReadOnlyList<EntityId> GetAllEntityIds();
}

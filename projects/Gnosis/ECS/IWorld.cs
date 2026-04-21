using Gnosis.Core;

namespace Gnosis.ECS;

public interface IWorld
{
    EntityId CreateEntity();
    void DestroyEntity(EntityId entityId);

    void AddComponent<T>(EntityId entityId, T component) where T : struct;
    T GetComponent<T>(EntityId entityId) where T : struct;
    void RemoveComponent<T>(EntityId entityId) where T : struct;
    bool HasComponent<T>(EntityId entityId) where T : struct;

    IQuery CreateQuery();
    IArchetype GetArchetype(params Type[] componentTypes);

    int EntityCount { get; }
}

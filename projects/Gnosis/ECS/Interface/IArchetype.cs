using Gnosis.ECS.Core;

namespace Gnosis.ECS.Interface;

public interface IArchetype
{
    IReadOnlySet<Type> ComponentTypes { get; }
    int EntityCount { get; }

    bool HasComponent<T>() where T : struct;
    IEnumerable<EntityId> GetEntities();
}

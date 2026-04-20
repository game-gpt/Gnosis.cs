namespace Gnosis.ECS;

public interface IArchetype
{
    IReadOnlySet<Type> ComponentTypes { get; }
    int EntityCount { get; }
    
    bool HasComponent<T>() where T : struct;
    IEnumerable<Guid> GetEntities();
}

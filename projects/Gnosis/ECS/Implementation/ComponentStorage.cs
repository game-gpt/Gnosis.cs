using Gnosis.ECS.Core;
using Gnosis.ECS.Interface;

namespace Gnosis.ECS.Implementation;

public class ComponentStorage : IComponentStorage
{
    private readonly Dictionary<Type, IComponentPool> _pools;

    public ComponentStorage()
    {
        _pools = new Dictionary<Type, IComponentPool>();
    }

    public IComponentPool GetPool<T>() where T : struct
    {
        var type = typeof(T);
        if (!_pools.TryGetValue(type, out var pool))
        {
            pool = new ComponentPool<T>();
            _pools[type] = pool;
        }
        return pool;
    }

    public IArchetype GetArchetypeStorage(params Type[] componentTypes)
    {
        if (componentTypes == null || componentTypes.Length == 0)
        {
            throw new ArgumentException("组件类型列表不能为空");
        }

        var typeSet = new HashSet<Type>(componentTypes);
        var matchingEntities = FindEntitiesWithAllTypes(componentTypes);
        return new SimpleArchetype(typeSet, matchingEntities);
    }

    private IReadOnlyList<EntityId> FindEntitiesWithAllTypes(Type[] componentTypes)
    {
        if (!_pools.TryGetValue(componentTypes[0], out var firstPool))
        {
            return [];
        }

        var firstEntityIds = firstPool.GetAllEntityIds();
        if (firstEntityIds.Count == 0)
        {
            return [];
        }

        var candidates = new HashSet<EntityId>(firstEntityIds);

        for (var i = 1; i < componentTypes.Length; i++)
        {
            if (!_pools.TryGetValue(componentTypes[i], out var pool))
            {
                return [];
            }

            var entityIds = pool.GetAllEntityIds();
            if (entityIds.Count == 0)
            {
                return [];
            }

            candidates.IntersectWith(entityIds);

            if (candidates.Count == 0)
            {
                return [];
            }
        }

        return candidates.ToList();
    }

    private class SimpleArchetype : IArchetype
    {
        private readonly HashSet<Type> _componentTypes;
        private readonly IReadOnlyList<EntityId> _entities;

        public SimpleArchetype(HashSet<Type> componentTypes, IReadOnlyList<EntityId> entities)
        {
            _componentTypes = componentTypes;
            _entities = entities;
        }

        public IReadOnlySet<Type> ComponentTypes => _componentTypes;

        public int EntityCount => _entities.Count;

        public bool HasComponent<T>() where T : struct
        {
            return _componentTypes.Contains(typeof(T));
        }

        public IEnumerable<EntityId> GetEntities()
        {
            return _entities;
        }
    }
}

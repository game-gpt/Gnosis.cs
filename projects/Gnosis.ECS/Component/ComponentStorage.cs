using Gnosis.ECS.Archetype;
using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Component;

/// <summary>
/// 组件存储实现，管理组件池和 Archetype 查询
/// </summary>
public class ComponentStorage : IComponentStorage
{
    private readonly Dictionary<Type, IComponentPool> _pools;

    public ComponentStorage()
    {
        _pools = new Dictionary<Type, IComponentPool>();
    }

    /// <summary>
    /// 获取或创建指定类型的组件池
    /// </summary>
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

    /// <summary>
    /// 获取匹配指定组件组合的 Archetype
    /// </summary>
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

        public ref T GetComponent<T>(EntityId entityId) where T : struct
        {
            throw new NotSupportedException("SimpleArchetype 不支持直接组件访问，请使用 ArchetypeManager");
        }

        public void SetComponent<T>(EntityId entityId, T component) where T : struct
        {
            throw new NotSupportedException("SimpleArchetype 不支持直接组件设置，请使用 ArchetypeManager");
        }
    }
}

using Gnosis.ECS.Archetype;
using Gnosis.ECS.Component;
using Gnosis.ECS.Entity;
using Gnosis.ECS.Query;
using Gnosis.ECS.System;

namespace Gnosis.ECS.World;

/// <summary>
/// 世界容器，ECS 的顶层入口，整合 EntityManager、ComponentManager、ArchetypeManager
/// </summary>
public sealed class World : IWorld
{
    #region 字段

    private readonly EntityManager _entityManager;
    private readonly ComponentManager _componentManager;
    private readonly ArchetypeManager _archetypeManager;
    private readonly SystemScheduler _systemScheduler;
    private readonly Dictionary<EntityId, HashSet<Type>> _entityComponentTypes = new();

    #endregion

    #region 属性

    public EntityManager Entities => _entityManager;
    public ComponentManager Components => _componentManager;
    public ArchetypeManager Archetypes => _archetypeManager;
    public SystemScheduler Systems => _systemScheduler;
    public int EntityCount => _entityManager.AliveCount;

    #endregion

    #region 构造函数

    public World()
    {
        var typeIdRegistry = new ComponentTypeId();
        _entityManager = new EntityManager();
        _componentManager = new ComponentManager(typeIdRegistry);
        _archetypeManager = new ArchetypeManager(typeIdRegistry);
        _systemScheduler = new SystemScheduler();
    }

    #endregion

    #region 公开方法 - 实体管理

    public EntityId CreateEntity()
    {
        var entityId = _entityManager.CreateEntity();
        _entityComponentTypes[entityId] = new HashSet<Type>();
        var archetype = _archetypeManager.GetOrCreate(_entityComponentTypes[entityId]);
        _archetypeManager.AssignArchetype(entityId, archetype);
        return entityId;
    }

    public void DestroyEntity(EntityId entityId)
    {
        if (!_entityManager.IsAlive(entityId)) return;

        _archetypeManager.RemoveEntity(entityId);
        _componentManager.OnEntityDestroyed(entityId);
        _entityManager.DestroyEntity(entityId);
        _entityComponentTypes.Remove(entityId);
    }

    #endregion

    #region 公开方法 - 组件管理

    public void AddComponent<T>(EntityId entityId, T component) where T : struct
    {
        if (!_entityManager.IsAlive(entityId)) return;

        _componentManager.Add(entityId, component);

        if (_entityComponentTypes.TryGetValue(entityId, out var types))
        {
            types.Add(typeof(T));
            _archetypeManager.MigrateEntity(entityId, types);
        }
    }

    public T GetComponent<T>(EntityId entityId) where T : struct
    {
        return _componentManager.Get<T>(entityId);
    }

    public void RemoveComponent<T>(EntityId entityId) where T : struct
    {
        if (!_entityManager.IsAlive(entityId)) return;

        _componentManager.Remove<T>(entityId);

        if (_entityComponentTypes.TryGetValue(entityId, out var types))
        {
            types.Remove(typeof(T));
            _archetypeManager.MigrateEntity(entityId, types);
        }
    }

    public bool HasComponent<T>(EntityId entityId) where T : struct
    {
        return _componentManager.Has<T>(entityId);
    }

    #endregion

    #region 公开方法 - 查询

    public IQuery CreateQuery()
    {
        return new EntityQuery(_archetypeManager);
    }

    public IArchetype GetArchetype(params Type[] componentTypes)
    {
        return _archetypeManager.GetOrCreate(componentTypes);
    }

    #endregion

    #region 公开方法 - 系统调度

    public void Update(float delta)
    {
        _systemScheduler.Update(delta);
    }

    #endregion
}

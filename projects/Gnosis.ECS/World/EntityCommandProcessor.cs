using Gnosis.ECS.Archetype;
using Gnosis.ECS.Component;
using Gnosis.ECS.Entity;
using Gnosis.ECS.Observer;
using Gnosis.ECS.Query;

namespace Gnosis.ECS.World;

/// <summary>
/// 实体命令处理器，统一协调实体和组件的增删改操作。
/// 负责维护 EntityManager、ComponentManager、ArchetypeManager、
/// ComponentVersionTracker 和 EntityObserver 之间的一致性。
/// </summary>
public sealed class EntityCommandProcessor
{
    #region 字段

    private readonly EntityManager _entityManager;
    private readonly ComponentManager _componentManager;
    private readonly ArchetypeManager _archetypeManager;
    private readonly ComponentVersionTracker _versionTracker;
    private readonly EntityObserver _observer;
    private readonly Dictionary<EntityId, HashSet<Type>> _entityComponentTypes;

    #endregion

    #region 构造函数

    public EntityCommandProcessor(
        EntityManager entityManager,
        ComponentManager componentManager,
        ArchetypeManager archetypeManager,
        ComponentVersionTracker versionTracker,
        EntityObserver observer)
    {
        _entityManager = entityManager;
        _componentManager = componentManager;
        _archetypeManager = archetypeManager;
        _versionTracker = versionTracker;
        _observer = observer;
        _entityComponentTypes = new Dictionary<EntityId, HashSet<Type>>();
    }

    #endregion

    #region 实体管理

    /// <summary>
    /// 创建新实体，初始化 Archetype 映射
    /// </summary>
    public EntityId CreateEntity()
    {
        var entityId = _entityManager.CreateEntity();
        _entityComponentTypes[entityId] = new HashSet<Type>();
        var archetype = _archetypeManager.GetOrCreate(_entityComponentTypes[entityId]);
        _archetypeManager.AssignArchetype(entityId, archetype);

        return entityId;
    }

    /// <summary>
    /// 销毁指定实体，清理所有关联数据
    /// </summary>
    public void DestroyEntity(EntityId entityId)
    {
        if (!_entityManager.IsAlive(entityId))
        {
            return;
        }

        var componentTypes = _entityComponentTypes.GetValueOrDefault(entityId);

        if (componentTypes != null)
        {
            foreach (var type in componentTypes)
            {
                _observer.NotifyRemoved(type, entityId);
            }
        }

        _archetypeManager.RemoveEntity(entityId);
        _componentManager.OnEntityDestroyed(entityId);
        _versionTracker.OnEntityDestroyed(entityId);
        _entityManager.DestroyEntity(entityId);
        _entityComponentTypes.Remove(entityId);
    }

    #endregion

    #region 组件管理

    /// <summary>
    /// 添加组件到指定实体，自动迁移 Archetype 并通知观察者
    /// </summary>
    public void AddComponent<T>(EntityId entityId, T component) where T : struct
    {
        if (!_entityManager.IsAlive(entityId))
        {
            return;
        }

        _componentManager.Add(entityId, component);

        if (_entityComponentTypes.TryGetValue(entityId, out var types))
        {
            types.Add(typeof(T));
            _archetypeManager.MigrateEntity(entityId, types);
        }

        _versionTracker.MarkChanged<T>(entityId);
        _observer.NotifyAdded<T>(entityId);
    }

    /// <summary>
    /// 设置指定实体的组件值，自动标记变更并通知观察者
    /// </summary>
    public void SetComponent<T>(EntityId entityId, T component) where T : struct
    {
        if (!_entityManager.IsAlive(entityId))
        {
            return;
        }

        _componentManager.Add(entityId, component);
        _versionTracker.MarkChanged<T>(entityId);
        _observer.NotifyChanged<T>(entityId);
    }

    /// <summary>
    /// 移除指定实体的组件，自动迁移 Archetype 并通知观察者
    /// </summary>
    public void RemoveComponent<T>(EntityId entityId) where T : struct
    {
        if (!_entityManager.IsAlive(entityId))
        {
            return;
        }

        _observer.NotifyRemoved<T>(entityId);
        _componentManager.Remove<T>(entityId);

        if (_entityComponentTypes.TryGetValue(entityId, out var types))
        {
            types.Remove(typeof(T));
            _archetypeManager.MigrateEntity(entityId, types);
        }
    }

    #endregion
}

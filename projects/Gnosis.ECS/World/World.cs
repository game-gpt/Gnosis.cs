using Gnosis.ECS.Archetype;
using Gnosis.ECS.Component;
using Gnosis.ECS.Entity;
using Gnosis.ECS.Observer;
using Gnosis.ECS.Query;
using Gnosis.ECS.System;

namespace Gnosis.ECS.World;

/// <summary>
/// 世界容器，ECS 的顶层入口，整合 EntityManager、ComponentManager、ArchetypeManager、
/// ComponentVersionTracker，支持变更追踪的查询和组件操作
/// </summary>
public sealed class World : IWorld
{
    #region 字段

    private readonly EntityManager _entityManager;
    private readonly ComponentManager _componentManager;
    private readonly ArchetypeManager _archetypeManager;
    private readonly SystemScheduler _systemScheduler;
    private readonly ComponentVersionTracker _versionTracker;
    private readonly EntityObserver _observer;
    private readonly Dictionary<EntityId, HashSet<Type>> _entityComponentTypes = new();

    #endregion

    #region 属性

    /// <summary>
    /// 实体管理器
    /// </summary>
    public EntityManager Entities => _entityManager;

    /// <summary>
    /// 组件管理器
    /// </summary>
    public ComponentManager Components => _componentManager;

    /// <summary>
    /// Archetype 管理器
    /// </summary>
    public ArchetypeManager Archetypes => _archetypeManager;

    /// <summary>
    /// 系统调度器
    /// </summary>
    public SystemScheduler Systems => _systemScheduler;

    /// <summary>
    /// 组件变更追踪器
    /// </summary>
    public ComponentVersionTracker VersionTracker => _versionTracker;

    /// <summary>
    /// 实体观察者
    /// </summary>
    public EntityObserver Observer => _observer;

    /// <summary>
    /// 活跃实体数量
    /// </summary>
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
        _versionTracker = new ComponentVersionTracker();
        _observer = new EntityObserver();
    }

    #endregion

    #region 公开方法 - 实体管理

    /// <summary>
    /// 创建新实体
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
    /// 销毁指定实体
    /// </summary>
    public void DestroyEntity(EntityId entityId)
    {
        if (!_entityManager.IsAlive(entityId)) return;

        var componentTypes = _entityComponentTypes.GetValueOrDefault(entityId);

        if (componentTypes != null)
        {
            foreach (var type in componentTypes)
            {
                _observer.NotifyRemoved(entityId, type);
            }
        }

        _archetypeManager.RemoveEntity(entityId);
        _componentManager.OnEntityDestroyed(entityId);
        _versionTracker.OnEntityDestroyed(entityId);
        _entityManager.DestroyEntity(entityId);
        _entityComponentTypes.Remove(entityId);
    }

    #endregion

    #region 公开方法 - 组件管理

    /// <summary>
    /// 添加组件到指定实体，自动标记变更并通知观察者
    /// </summary>
    public void AddComponent<T>(EntityId entityId, T component) where T : struct
    {
        if (!_entityManager.IsAlive(entityId)) return;

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
    /// 获取指定实体的组件
    /// </summary>
    public T GetComponent<T>(EntityId entityId) where T : struct
    {
        return _componentManager.Get<T>(entityId);
    }

    /// <summary>
    /// 设置指定实体的组件值，自动标记变更并通知观察者
    /// </summary>
    public void SetComponent<T>(EntityId entityId, T component) where T : struct
    {
        if (!_entityManager.IsAlive(entityId)) return;

        _componentManager.Add(entityId, component);

        var archetype = _archetypeManager.GetArchetypeForEntity(entityId);

        if (archetype != null)
        {
            archetype.SetComponent(entityId, component);
        }

        _versionTracker.MarkChanged<T>(entityId);
        _observer.NotifyChanged<T>(entityId);
    }

    /// <summary>
    /// 移除指定实体的组件，自动通知观察者
    /// </summary>
    public void RemoveComponent<T>(EntityId entityId) where T : struct
    {
        if (!_entityManager.IsAlive(entityId)) return;

        _observer.NotifyRemoved<T>(entityId);

        _componentManager.Remove<T>(entityId);

        if (_entityComponentTypes.TryGetValue(entityId, out var types))
        {
            types.Remove(typeof(T));
            _archetypeManager.MigrateEntity(entityId, types);
        }
    }

    /// <summary>
    /// 检查指定实体是否拥有此类型组件
    /// </summary>
    public bool HasComponent<T>(EntityId entityId) where T : struct
    {
        return _componentManager.Has<T>(entityId);
    }

    #endregion

    #region 公开方法 - 查询

    /// <summary>
    /// 创建查询构建器，集成变更追踪
    /// </summary>
    public IQuery CreateQuery()
    {
        return new EntityQuery(_archetypeManager, _versionTracker);
    }

    /// <summary>
    /// 基于查询描述符创建查询构建器
    /// </summary>
    public IQuery CreateQuery(QueryDescription description)
    {
        var query = new EntityQuery(_archetypeManager, _versionTracker);

        foreach (var type in description.AllTypes)
        {
            query.All(type);
        }

        foreach (var type in description.AnyTypes)
        {
            query.Any(type);
        }

        foreach (var type in description.NoneTypes)
        {
            query.None(type);
        }

        foreach (var type in description.ChangedTypes)
        {
            query.Changed(type);
        }

        return query;
    }

    /// <summary>
    /// 获取匹配指定组件组合的 Archetype
    /// </summary>
    public IArchetype GetArchetype(params Type[] componentTypes)
    {
        return _archetypeManager.GetOrCreate(componentTypes);
    }

    #endregion

    #region 公开方法 - 系统调度

    /// <summary>
    /// 更新世界，执行所有活跃系统
    /// </summary>
    public void Update(float delta)
    {
        _systemScheduler.Update(delta);
    }

    #endregion
}

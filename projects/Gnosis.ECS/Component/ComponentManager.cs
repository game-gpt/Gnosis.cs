using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Component;

/// <summary>
/// 组件管理器，统一管理所有组件类型的存储和操作。
/// 自动创建和管理组件池，提供类型安全的组件访问接口。
/// </summary>
public sealed class ComponentManager
{
    private readonly Dictionary<Type, IComponentPool> _pools;
    private readonly ComponentTypeId _typeIdRegistry;

    /// <summary>
    /// 组件类型 ID 注册表
    /// </summary>
    public ComponentTypeId TypeIdRegistry => _typeIdRegistry;

    /// <summary>
    /// 已注册的组件类型数量
    /// </summary>
    public int PoolCount => _pools.Count;

    public ComponentManager()
    {
        _pools = new Dictionary<Type, IComponentPool>();
        _typeIdRegistry = new ComponentTypeId();
    }

    public ComponentManager(ComponentTypeId typeIdRegistry)
    {
        _pools = new Dictionary<Type, IComponentPool>();
        _typeIdRegistry = typeIdRegistry;
    }

    /// <summary>
    /// 添加组件到指定实体
    /// </summary>
    public void Add<T>(EntityId entityId, T component) where T : struct
    {
        var pool = GetOrCreatePool<T>();
        pool.Add(entityId, component);
    }

    /// <summary>
    /// 获取指定实体的组件
    /// </summary>
    public T Get<T>(EntityId entityId) where T : struct
    {
        var pool = GetPool<T>();

        if (pool == null)
        {
            throw new KeyNotFoundException($"组件类型 {typeof(T).Name} 未注册");
        }

        return pool.Get<T>(entityId);
    }

    /// <summary>
    /// 获取指定实体组件的引用
    /// </summary>
    public ref T GetRef<T>(EntityId entityId) where T : struct
    {
        var pool = GetPool<T>();

        if (pool == null)
        {
            throw new KeyNotFoundException($"组件类型 {typeof(T).Name} 未注册");
        }

        if (pool is ComponentPool<T> typedPool)
        {
            return ref typedPool.GetRef(entityId);
        }

        throw new InvalidOperationException($"组件池类型不匹配：{typeof(T).Name}");
    }

    /// <summary>
    /// 检查指定实体是否拥有此类型组件
    /// </summary>
    public bool Has<T>(EntityId entityId) where T : struct
    {
        var pool = GetPool<T>();

        if (pool == null)
        {
            return false;
        }

        return pool.Has<T>(entityId);
    }

    /// <summary>
    /// 移除指定实体的组件
    /// </summary>
    public void Remove<T>(EntityId entityId) where T : struct
    {
        var pool = GetPool<T>();

        if (pool == null)
        {
            return;
        }

        pool.Remove<T>(entityId);
    }

    /// <summary>
    /// 获取或创建指定类型的组件池
    /// </summary>
    public IComponentPool GetOrCreatePool<T>() where T : struct
    {
        var type = typeof(T);

        if (!_pools.TryGetValue(type, out var pool))
        {
            pool = new ComponentPool<T>();
            _pools[type] = pool;
            _typeIdRegistry.GetOrRegister<T>();
        }

        return pool;
    }

    /// <summary>
    /// 获取指定类型的组件池，不存在返回 null
    /// </summary>
    public IComponentPool? GetPool<T>() where T : struct
    {
        return _pools.TryGetValue(typeof(T), out var pool) ? pool : null;
    }

    /// <summary>
    /// 获取指定类型的组件池（非泛型），不存在返回 null
    /// </summary>
    public IComponentPool? GetPool(Type componentType)
    {
        return _pools.TryGetValue(componentType, out var pool) ? pool : null;
    }

    /// <summary>
    /// 获取或创建指定类型的组件池（非泛型）
    /// </summary>
    public IComponentPool GetOrCreatePool(Type componentType)
    {
        if (!_pools.TryGetValue(componentType, out var pool))
        {
            var poolType = typeof(ComponentPool<>).MakeGenericType(componentType);
            pool = (IComponentPool)Activator.CreateInstance(poolType, (object)1024)!;
            _pools[componentType] = pool;
            _typeIdRegistry.GetOrRegister(componentType);
        }

        return pool;
    }

    /// <summary>
    /// 实体销毁时清理其所有组件
    /// </summary>
    public void OnEntityDestroyed(EntityId entityId)
    {
        foreach (var pool in _pools.Values)
        {
            if (pool.HasEntity(entityId))
            {
                pool.RemoveEntity(entityId);
            }
        }
    }
}

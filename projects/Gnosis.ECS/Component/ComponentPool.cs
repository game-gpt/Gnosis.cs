using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Component;

/// <summary>
/// 基于 Sparse Set 的组件池，提供 O(1) 的添加、获取、移除操作。
/// 组件数据在稠密数组中连续存储，保证缓存友好的迭代性能。
/// </summary>
public class ComponentPool<T> : IComponentPool where T : struct
{
    private int[] _sparse;
    private T[] _dense;
    private EntityId[] _entities;
    private int _count;

    /// <summary>
    /// 组件类型
    /// </summary>
    public Type ComponentType => typeof(T);

    /// <summary>
    /// 组件数量
    /// </summary>
    public int Count => _count;

    public ComponentPool(int initialCapacity = 1024)
    {
        _sparse = new int[initialCapacity];
        Array.Fill(_sparse, -1);
        _dense = new T[initialCapacity];
        _entities = new EntityId[initialCapacity];
        _count = 0;
    }

    /// <summary>
    /// 添加组件到指定实体，若已存在则替换
    /// </summary>
    public void Add<TComponent>(EntityId entityId, TComponent component) where TComponent : struct
    {
        if (typeof(TComponent) != typeof(T))
        {
            throw new InvalidOperationException($"组件类型不匹配：期望 {typeof(T).Name}，实际 {typeof(TComponent).Name}");
        }

        var index = (int)entityId.Index;

        if (index >= _sparse.Length)
        {
            GrowSparse(index * 2);
        }

        if (_sparse[index] != -1)
        {
            _dense[_sparse[index]] = (T)(object)component;
            return;
        }

        if (_count >= _dense.Length)
        {
            GrowDense(_dense.Length * 2);
        }

        _sparse[index] = _count;
        _dense[_count] = (T)(object)component;
        _entities[_count] = entityId;
        _count++;
    }

    /// <summary>
    /// 获取指定实体的组件
    /// </summary>
    public TComponent Get<TComponent>(EntityId entityId) where TComponent : struct
    {
        if (typeof(TComponent) != typeof(T))
        {
            throw new InvalidOperationException($"组件类型不匹配：期望 {typeof(T).Name}，实际 {typeof(TComponent).Name}");
        }

        var index = (int)entityId.Index;

        if (index >= _sparse.Length || _sparse[index] == -1)
        {
            throw new KeyNotFoundException($"实体 {entityId} 没有组件 {typeof(T).Name}");
        }

        return (TComponent)(object)_dense[_sparse[index]];
    }

    /// <summary>
    /// 获取指定实体组件的引用
    /// </summary>
    public ref T GetRef(EntityId entityId)
    {
        var index = (int)entityId.Index;

        if (index >= _sparse.Length || _sparse[index] == -1)
        {
            throw new KeyNotFoundException($"实体 {entityId} 没有组件 {typeof(T).Name}");
        }

        return ref _dense[_sparse[index]];
    }

    /// <summary>
    /// 移除指定实体的组件，使用 swap-back 策略保持稠密数组连续
    /// </summary>
    public void Remove<TComponent>(EntityId entityId) where TComponent : struct
    {
        if (typeof(TComponent) != typeof(T))
        {
            throw new InvalidOperationException($"组件类型不匹配：期望 {typeof(T).Name}，实际 {typeof(TComponent).Name}");
        }

        RemoveEntity(entityId);
    }

    /// <summary>
    /// 检查指定实体是否拥有此类型组件
    /// </summary>
    public bool Has<TComponent>(EntityId entityId) where TComponent : struct
    {
        if (typeof(TComponent) != typeof(T))
        {
            return false;
        }

        return HasEntity(entityId);
    }

    /// <summary>
    /// 获取所有组件的只读列表
    /// </summary>
    public IReadOnlyList<T> GetAll()
    {
        return new ArraySegment<T>(_dense, 0, _count);
    }

    /// <summary>
    /// 获取所有拥有此组件的实体 ID
    /// </summary>
    public IReadOnlyList<EntityId> GetAllEntityIds()
    {
        return new ArraySegment<EntityId>(_entities, 0, _count);
    }

    /// <summary>
    /// 检查指定实体是否在此池中（非泛型版本）
    /// </summary>
    public bool HasEntity(EntityId entityId)
    {
        var index = (int)entityId.Index;

        if (index >= _sparse.Length)
        {
            return false;
        }

        return _sparse[index] != -1;
    }

    /// <summary>
    /// 移除指定实体的组件（非泛型版本），使用 swap-back 策略
    /// </summary>
    public void RemoveEntity(EntityId entityId)
    {
        var index = (int)entityId.Index;

        if (index >= _sparse.Length || _sparse[index] == -1)
        {
            return;
        }

        var denseIndex = _sparse[index];
        var lastIndex = _count - 1;

        if (denseIndex != lastIndex)
        {
            _dense[denseIndex] = _dense[lastIndex];
            _entities[denseIndex] = _entities[lastIndex];
            _sparse[_entities[denseIndex].Index] = denseIndex;
        }

        _dense[lastIndex] = default;
        _entities[lastIndex] = default;
        _sparse[index] = -1;
        _count--;
    }

    private void GrowSparse(int newCapacity)
    {
        var newSparse = new int[newCapacity];
        Array.Fill(newSparse, -1);
        Array.Copy(_sparse, newSparse, _sparse.Length);
        _sparse = newSparse;
    }

    private void GrowDense(int newCapacity)
    {
        var newDense = new T[newCapacity];
        var newEntities = new EntityId[newCapacity];
        Array.Copy(_dense, newDense, _count);
        Array.Copy(_entities, newEntities, _count);
        _dense = newDense;
        _entities = newEntities;
    }
}

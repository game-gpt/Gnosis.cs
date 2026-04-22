using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Archetype;

/// <summary>
/// 分块内存布局，将同一 Archetype 的实体数据按固定大小的块存储。
/// 组件数据在内存中连续排列，提升缓存命中率。
/// 每个 Chunk 存储固定数量的实体及其所有组件数据。
/// </summary>
public sealed class Chunk
{
    #region 常量

    /// <summary>
    /// 默认 Chunk 容量
    /// </summary>
    public const int DefaultCapacity = 256;

    #endregion

    #region 字段

    private readonly Dictionary<Type, Array> _componentArrays;
    private readonly HashSet<Type> _componentTypesSet;
    private readonly EntityId[] _entities;
    private readonly int _capacity;
    private int _count;

    #endregion

    #region 属性

    /// <summary>
    /// 当前 Chunk 中的实体数量
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Chunk 的最大容量
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Chunk 是否已满
    /// </summary>
    public bool IsFull => _count >= _capacity;

    /// <summary>
    /// 此 Chunk 包含的组件类型集合
    /// </summary>
    public IReadOnlySet<Type> ComponentTypes => _componentTypesSet;

    #endregion

    #region 构造函数

    public Chunk(IEnumerable<Type> componentTypes, int capacity = DefaultCapacity)
    {
        _capacity = capacity;
        _entities = new EntityId[capacity];
        _componentArrays = new Dictionary<Type, Array>();
        _componentTypesSet = new HashSet<Type>(componentTypes);

        foreach (var type in _componentTypesSet)
        {
            _componentArrays[type] = Array.CreateInstance(type, capacity);
        }

        _count = 0;
    }

    #endregion

    #region 实体管理

    /// <summary>
    /// 添加实体到 Chunk，返回实体在 Chunk 中的索引
    /// </summary>
    public int AddEntity(EntityId entityId)
    {
        if (IsFull)
        {
            return -1;
        }

        var index = _count;
        _entities[index] = entityId;
        _count++;

        return index;
    }

    /// <summary>
    /// 移除指定索引的实体，使用 swap-back 策略保持数据连续
    /// </summary>
    public void RemoveEntity(int index)
    {
        if (index < 0 || index >= _count)
        {
            return;
        }

        var lastIndex = _count - 1;

        if (index != lastIndex)
        {
            _entities[index] = _entities[lastIndex];

            foreach (var kvp in _componentArrays)
            {
                Array.Copy(kvp.Value, lastIndex, kvp.Value, index, 1);
            }
        }

        _entities[lastIndex] = default;
        _count--;
    }

    /// <summary>
    /// 获取指定索引的实体 ID
    /// </summary>
    public EntityId GetEntity(int index)
    {
        if (index < 0 || index >= _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"索引 {index} 超出范围 [0, {_count})");
        }

        return _entities[index];
    }

    /// <summary>
    /// 获取此 Chunk 中所有实体 ID
    /// </summary>
    public IReadOnlyList<EntityId> GetAllEntities()
    {
        return new ArraySegment<EntityId>(_entities, 0, _count);
    }

    /// <summary>
    /// 查找指定实体在 Chunk 中的索引，未找到返回 -1
    /// </summary>
    public int IndexOf(EntityId entityId)
    {
        for (var i = 0; i < _count; i++)
        {
            if (_entities[i] == entityId)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 清空 Chunk 中的所有实体和组件数据
    /// </summary>
    public void Clear()
    {
        Array.Clear(_entities, 0, _count);

        foreach (var kvp in _componentArrays)
        {
            Array.Clear(kvp.Value, 0, _count);
        }

        _count = 0;
    }

    #endregion

    #region 组件访问

    /// <summary>
    /// 获取指定索引实体的组件引用
    /// </summary>
    public ref T GetComponent<T>(int index) where T : struct
    {
        if (!_componentArrays.TryGetValue(typeof(T), out var array))
        {
            throw new KeyNotFoundException($"组件类型 {typeof(T).Name} 不在此 Chunk 中");
        }

        return ref ((T[])array)[index];
    }

    /// <summary>
    /// 设置指定索引实体的组件
    /// </summary>
    public void SetComponent<T>(int index, T component) where T : struct
    {
        if (!_componentArrays.TryGetValue(typeof(T), out var array))
        {
            throw new KeyNotFoundException($"组件类型 {typeof(T).Name} 不在此 Chunk 中");
        }

        ((T[])array)[index] = component;
    }

    /// <summary>
    /// 检查此 Chunk 是否包含指定类型的组件
    /// </summary>
    public bool HasComponent<T>() where T : struct
    {
        return _componentTypesSet.Contains(typeof(T));
    }

    /// <summary>
    /// 将指定索引实体的所有组件数据复制到目标 Chunk
    /// </summary>
    public void CopyEntityTo(int sourceIndex, Chunk targetChunk, int targetIndex)
    {
        foreach (var type in _componentTypesSet)
        {
            if (!targetChunk._componentArrays.TryGetValue(type, out var targetArray))
            {
                continue;
            }

            var sourceArray = _componentArrays[type];
            Array.Copy(sourceArray, sourceIndex, targetArray, targetIndex, 1);
        }
    }

    /// <summary>
    /// 获取指定组件类型的数组引用（用于批量操作）
    /// </summary>
    public T[] GetComponentArray<T>() where T : struct
    {
        if (!_componentArrays.TryGetValue(typeof(T), out var array))
        {
            throw new KeyNotFoundException($"组件类型 {typeof(T).Name} 不在此 Chunk 中");
        }

        return (T[])array;
    }

    #endregion
}

using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Archetype;

/// <summary>
/// Archetype 实现，表示具有相同组件组合的实体集合。
/// 使用 Chunk 分块存储组件数据，确保内存连续和缓存友好。
/// </summary>
public sealed class Archetype : IArchetype
{
    #region 字段

    private readonly HashSet<Type> _componentTypes;
    private readonly List<Chunk> _chunks;
    private readonly Dictionary<EntityId, EntityLocation> _entityLocations;
    private readonly int _chunkCapacity;

    #endregion

    #region 属性

    /// <summary>
    /// 此 Archetype 包含的组件类型集合
    /// </summary>
    public IReadOnlySet<Type> ComponentTypes => _componentTypes;

    /// <summary>
    /// 此 Archetype 中的实体数量
    /// </summary>
    public int EntityCount => _entityLocations.Count;

    /// <summary>
    /// 此 Archetype 的 Chunk 列表
    /// </summary>
    public IReadOnlyList<Chunk> Chunks => _chunks;

    /// <summary>
    /// 每个 Chunk 的容量
    /// </summary>
    public int ChunkCapacity => _chunkCapacity;

    #endregion

    #region 构造函数

    public Archetype(IEnumerable<Type> componentTypes, int chunkCapacity = Chunk.DefaultCapacity)
    {
        _componentTypes = new HashSet<Type>(componentTypes);
        _chunks = new List<Chunk>();
        _entityLocations = new Dictionary<EntityId, EntityLocation>();
        _chunkCapacity = chunkCapacity;

        AddNewChunk();
    }

    #endregion

    #region 实体管理

    /// <summary>
    /// 添加实体到此 Archetype，返回实体位置信息
    /// </summary>
    public EntityLocation AddEntity(EntityId entityId)
    {
        var chunk = GetOrCreateAvailableChunk();
        var indexInChunk = chunk.AddEntity(entityId);

        if (indexInChunk == -1)
        {
            chunk = AddNewChunk();
            indexInChunk = chunk.AddEntity(entityId);
        }

        var location = new EntityLocation(_chunks.Count - 1, indexInChunk);
        _entityLocations[entityId] = location;

        return location;
    }

    /// <summary>
    /// 移除实体，使用 swap-back 策略保持 Chunk 数据连续
    /// </summary>
    public bool RemoveEntity(EntityId entityId)
    {
        if (!_entityLocations.TryGetValue(entityId, out var location))
        {
            return false;
        }

        var chunk = _chunks[location.ChunkIndex];
        var lastIndex = chunk.Count - 1;

        if (location.IndexInChunk != lastIndex)
        {
            var swappedEntityId = chunk.GetEntity(lastIndex);

            _entityLocations[swappedEntityId] = new EntityLocation(location.ChunkIndex, location.IndexInChunk);
        }

        chunk.RemoveEntity(location.IndexInChunk);
        _entityLocations.Remove(entityId);

        return true;
    }

    /// <summary>
    /// 获取实体在此 Archetype 中的位置信息
    /// </summary>
    public EntityLocation? GetEntityLocation(EntityId entityId)
    {
        return _entityLocations.TryGetValue(entityId, out var location) ? location : null;
    }

    /// <summary>
    /// 检查此 Archetype 是否包含指定实体
    /// </summary>
    public bool ContainsEntity(EntityId entityId)
    {
        return _entityLocations.ContainsKey(entityId);
    }

    /// <summary>
    /// 获取此 Archetype 中的所有实体 ID
    /// </summary>
    public IEnumerable<EntityId> GetEntities()
    {
        foreach (var chunk in _chunks)
        {
            var entities = chunk.GetAllEntities();

            for (var i = 0; i < entities.Count; i++)
            {
                yield return entities[i];
            }
        }
    }

    #endregion

    #region 组件访问

    /// <summary>
    /// 检查是否包含指定类型的组件
    /// </summary>
    public bool HasComponent<T>() where T : struct
    {
        return _componentTypes.Contains(typeof(T));
    }

    /// <summary>
    /// 获取指定实体的组件引用
    /// </summary>
    public ref T GetComponent<T>(EntityId entityId) where T : struct
    {
        var location = GetEntityLocation(entityId);

        if (location == null)
        {
            throw new KeyNotFoundException($"实体 {entityId} 不在此 Archetype 中");
        }

        return ref _chunks[location.Value.ChunkIndex].GetComponent<T>(location.Value.IndexInChunk);
    }

    /// <summary>
    /// 设置指定实体的组件
    /// </summary>
    public void SetComponent<T>(EntityId entityId, T component) where T : struct
    {
        var location = GetEntityLocation(entityId);

        if (location == null)
        {
            throw new KeyNotFoundException($"实体 {entityId} 不在此 Archetype 中");
        }

        _chunks[location.Value.ChunkIndex].SetComponent(location.Value.IndexInChunk, component);
    }

    /// <summary>
    /// 判断此 Archetype 的组件类型是否与给定集合完全匹配
    /// </summary>
    public bool Matches(IReadOnlySet<Type> types)
    {
        return _componentTypes.SetEquals(types);
    }

    /// <summary>
    /// 判断此 Archetype 的组件类型是否为给定集合的子集
    /// </summary>
    public bool IsSubsetOf(IReadOnlySet<Type> types)
    {
        return _componentTypes.IsSubsetOf(types);
    }

    #endregion

    #region 私有方法

    private Chunk GetOrCreateAvailableChunk()
    {
        if (_chunks.Count > 0 && !_chunks[_chunks.Count - 1].IsFull)
        {
            return _chunks[_chunks.Count - 1];
        }

        return AddNewChunk();
    }

    private Chunk AddNewChunk()
    {
        var chunk = new Chunk(_componentTypes, _chunkCapacity);
        _chunks.Add(chunk);
        return chunk;
    }

    #endregion
}

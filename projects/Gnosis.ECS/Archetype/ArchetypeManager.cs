using Gnosis.ECS.Component;
using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Archetype;

/// <summary>
/// Archetype 管理器，负责 Archetype 的查找、创建、缓存和实体迁移。
/// 支持组件添加/删除时的 Archetype 迁移，自动转移组件数据。
/// </summary>
public sealed class ArchetypeManager
{
    #region 字段

    private readonly Dictionary<long, Archetype> _archetypes;
    private readonly Dictionary<EntityId, Archetype> _entityArchetypes;
    private readonly ComponentTypeId _typeIdRegistry;
    private readonly ChunkPool? _chunkPool;

    #endregion

    #region 属性

    /// <summary>
    /// 已创建的 Archetype 数量
    /// </summary>
    public int ArchetypeCount => _archetypes.Count;

    #endregion

    #region 构造函数

    public ArchetypeManager(ComponentTypeId typeIdRegistry)
    {
        _archetypes = new Dictionary<long, Archetype>();
        _entityArchetypes = new Dictionary<EntityId, Archetype>();
        _typeIdRegistry = typeIdRegistry;
        _chunkPool = null;
    }

    public ArchetypeManager(ComponentTypeId typeIdRegistry, ChunkPool chunkPool)
    {
        _archetypes = new Dictionary<long, Archetype>();
        _entityArchetypes = new Dictionary<EntityId, Archetype>();
        _typeIdRegistry = typeIdRegistry;
        _chunkPool = chunkPool;
    }

    #endregion

    #region Archetype 查找与创建

    /// <summary>
    /// 获取或创建指定组件类型组合的 Archetype
    /// </summary>
    public Archetype GetOrCreate(IEnumerable<Type> componentTypes)
    {
        var typeSet = new HashSet<Type>(componentTypes);
        var hash = ComputeArchetypeHash(typeSet);

        if (_archetypes.TryGetValue(hash, out var archetype))
        {
            return archetype;
        }

        archetype = new Archetype(typeSet);
        _archetypes[hash] = archetype;

        return archetype;
    }

    /// <summary>
    /// 查找指定组件类型组合的 Archetype，不存在返回 null
    /// </summary>
    public Archetype? Find(IEnumerable<Type> componentTypes)
    {
        var typeSet = new HashSet<Type>(componentTypes);
        var hash = ComputeArchetypeHash(typeSet);

        return _archetypes.GetValueOrDefault(hash);
    }

    /// <summary>
    /// 获取所有 Archetype
    /// </summary>
    public IEnumerable<Archetype> GetArchetypes()
    {
        return _archetypes.Values;
    }

    #endregion

    #region 实体-Archetype 映射

    /// <summary>
    /// 将实体分配到指定 Archetype
    /// </summary>
    public void AssignArchetype(EntityId entityId, Archetype archetype)
    {
        _entityArchetypes[entityId] = archetype;
        archetype.AddEntity(entityId);
    }

    /// <summary>
    /// 获取实体所属的 Archetype
    /// </summary>
    public Archetype? GetArchetypeForEntity(EntityId entityId)
    {
        return _entityArchetypes.GetValueOrDefault(entityId);
    }

    /// <summary>
    /// 从 Archetype 中移除实体
    /// </summary>
    public void RemoveEntity(EntityId entityId)
    {
        if (_entityArchetypes.TryGetValue(entityId, out var archetype))
        {
            archetype.RemoveEntity(entityId);
            _entityArchetypes.Remove(entityId);
        }
    }

    #endregion

    #region 组件添加/删除迁移

    /// <summary>
    /// 为实体添加组件类型，迁移到新的 Archetype。
    /// 需要外部调用者负责设置新组件的初始值。
    /// </summary>
    public Archetype AddComponentType<T>(EntityId entityId) where T : struct
    {
        var oldArchetype = GetArchetypeForEntity(entityId);
        var oldTypes = oldArchetype?.ComponentTypes ?? new HashSet<Type>();
        var newTypes = new HashSet<Type>(oldTypes) { typeof(T) };

        return MigrateEntity(entityId, oldArchetype, newTypes);
    }

    /// <summary>
    /// 为实体移除组件类型，迁移到新的 Archetype。
    /// </summary>
    public Archetype RemoveComponentType<T>(EntityId entityId) where T : struct
    {
        var oldArchetype = GetArchetypeForEntity(entityId);
        var oldTypes = oldArchetype?.ComponentTypes ?? new HashSet<Type>();
        var newTypes = new HashSet<Type>(oldTypes);
        newTypes.Remove(typeof(T));

        return MigrateEntity(entityId, oldArchetype, newTypes);
    }

    /// <summary>
    /// 将实体迁移到新的 Archetype，自动转移共有的组件数据。
    /// </summary>
    public Archetype MigrateEntity(EntityId entityId, IEnumerable<Type> newComponentTypes)
    {
        var oldArchetype = GetArchetypeForEntity(entityId);
        var newTypes = new HashSet<Type>(newComponentTypes);

        return MigrateEntity(entityId, oldArchetype, newTypes);
    }

    #endregion

    #region 查询

    /// <summary>
    /// 查询匹配指定条件的 Archetype
    /// </summary>
    public IEnumerable<Archetype> QueryArchetypes(IReadOnlySet<Type> allTypes, IReadOnlySet<Type> anyTypes, IReadOnlySet<Type> noneTypes)
    {
        foreach (var archetype in _archetypes.Values)
        {
            if (allTypes.Count > 0 && !allTypes.IsSubsetOf(archetype.ComponentTypes))
            {
                continue;
            }

            if (anyTypes.Count > 0 && !anyTypes.Overlaps(archetype.ComponentTypes))
            {
                continue;
            }

            if (noneTypes.Count > 0 && noneTypes.Overlaps(archetype.ComponentTypes))
            {
                continue;
            }

            yield return archetype;
        }
    }

    #endregion

    #region 私有方法

    private Archetype MigrateEntity(EntityId entityId, Archetype? oldArchetype, HashSet<Type> newTypes)
    {
        var newArchetype = GetOrCreate(newTypes);

        if (oldArchetype != null)
        {
            var oldLocation = oldArchetype.GetEntityLocation(entityId);

            if (oldLocation != null)
            {
                var newLocation = newArchetype.AddEntity(entityId);
                var oldChunk = oldArchetype.Chunks[oldLocation.Value.ChunkIndex];
                var newChunk = newArchetype.Chunks[newLocation.ChunkIndex];

                oldChunk.CopyEntityTo(oldLocation.Value.IndexInChunk, newChunk, newLocation.IndexInChunk);

                oldArchetype.RemoveEntity(entityId);
            }
            else
            {
                newArchetype.AddEntity(entityId);
            }
        }
        else
        {
            newArchetype.AddEntity(entityId);
        }

        _entityArchetypes[entityId] = newArchetype;

        return newArchetype;
    }

    private long ComputeArchetypeHash(IReadOnlySet<Type> types)
    {
        long hash = 0;

        foreach (var type in types)
        {
            var id = _typeIdRegistry.GetOrRegister(type);
            hash = hash * 31 + id;
        }

        return hash;
    }

    #endregion
}

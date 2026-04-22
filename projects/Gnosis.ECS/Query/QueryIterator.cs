using ArchetypeEntity = Gnosis.ECS.Archetype.Archetype;
using Gnosis.ECS.Archetype;
using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Query;

/// <summary>
/// 带引用参数的组件迭代回调委托（单组件）
/// </summary>
public delegate void RefAction<T1>(EntityId entityId, ref T1 comp1) where T1 : struct;

/// <summary>
/// 带引用参数的组件迭代回调委托（双组件）
/// </summary>
public delegate void RefAction<T1, T2>(EntityId entityId, ref T1 comp1, ref T2 comp2)
    where T1 : struct
    where T2 : struct;

/// <summary>
/// 带引用参数的组件迭代回调委托（三组件）
/// </summary>
public delegate void RefAction<T1, T2, T3>(EntityId entityId, ref T1 comp1, ref T2 comp2, ref T3 comp3)
    where T1 : struct
    where T2 : struct
    where T3 : struct;

/// <summary>
/// 带引用参数的组件迭代回调委托（四组件）
/// </summary>
public delegate void RefAction<T1, T2, T3, T4>(EntityId entityId, ref T1 comp1, ref T2 comp2, ref T3 comp3, ref T4 comp4)
    where T1 : struct
    where T2 : struct
    where T3 : struct
    where T4 : struct;

/// <summary>
/// 查询迭代器，高效遍历匹配查询条件的实体和组件。
/// 直接从 Archetype 的 Chunk 中读取组件数据，避免间接寻址开销。
/// 支持 ref 返回值实现原地修改组件，支持变更过滤迭代。
/// </summary>
public sealed class QueryIterator
{
    #region 字段

    private readonly List<ArchetypeEntity> _matchingArchetypes;
    private readonly ComponentVersionTracker? _versionTracker;
    private readonly Dictionary<Type, uint>? _changedSinceVersions;

    #endregion

    #region 属性

    /// <summary>
    /// 匹配的 Archetype 数量
    /// </summary>
    public int ArchetypeCount => _matchingArchetypes.Count;

    #endregion

    #region 构造函数

    public QueryIterator(IEnumerable<ArchetypeEntity> archetypes)
    {
        _matchingArchetypes = new List<ArchetypeEntity>(archetypes);
        _versionTracker = null;
        _changedSinceVersions = null;
    }

    public QueryIterator(
        IEnumerable<ArchetypeEntity> archetypes,
        ComponentVersionTracker versionTracker,
        Dictionary<Type, uint> changedSinceVersions)
    {
        _matchingArchetypes = new List<ArchetypeEntity>(archetypes);
        _versionTracker = versionTracker;
        _changedSinceVersions = new Dictionary<Type, uint>(changedSinceVersions);
    }

    #endregion

    #region 实体迭代

    /// <summary>
    /// 遍历所有匹配的实体 ID
    /// </summary>
    public IEnumerable<EntityId> Entities()
    {
        foreach (var archetype in _matchingArchetypes)
        {
            foreach (var entityId in archetype.GetEntities())
            {
                if (ShouldIncludeEntity(entityId))
                {
                    yield return entityId;
                }
            }
        }
    }

    #endregion

    #region 单组件迭代

    /// <summary>
    /// 遍历匹配实体及其单个组件（只读）
    /// </summary>
    public IEnumerable<(EntityId Entity, T1 Comp1)> EntitiesWith<T1>() where T1 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            if (!archetype.HasComponent<T1>())
            {
                continue;
            }

            foreach (var chunk in archetype.Chunks)
            {
                if (!chunk.HasComponent<T1>())
                {
                    continue;
                }

                var entities = chunk.GetAllEntities();
                var compArray = chunk.GetComponentArray<T1>();

                for (var i = 0; i < entities.Count; i++)
                {
                    if (ShouldIncludeEntity(entities[i]))
                    {
                        yield return (entities[i], compArray[i]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 遍历匹配实体及其单个组件（引用返回，支持原地修改）
    /// </summary>
    public void EntitiesWithRef<T1>(RefAction<T1> action) where T1 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            if (!archetype.HasComponent<T1>())
            {
                continue;
            }

            foreach (var chunk in archetype.Chunks)
            {
                if (!chunk.HasComponent<T1>())
                {
                    continue;
                }

                var entities = chunk.GetAllEntities();
                var compArray = chunk.GetComponentArray<T1>();

                for (var i = 0; i < entities.Count; i++)
                {
                    if (ShouldIncludeEntity(entities[i]))
                    {
                        action(entities[i], ref compArray[i]);
                    }
                }
            }
        }
    }

    #endregion

    #region 双组件迭代

    /// <summary>
    /// 遍历匹配实体及其两个组件（只读）
    /// </summary>
    public IEnumerable<(EntityId Entity, T1 Comp1, T2 Comp2)> EntitiesWith<T1, T2>()
        where T1 : struct
        where T2 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            if (!archetype.HasComponent<T1>() || !archetype.HasComponent<T2>())
            {
                continue;
            }

            foreach (var chunk in archetype.Chunks)
            {
                var entities = chunk.GetAllEntities();
                var comp1Array = chunk.GetComponentArray<T1>();
                var comp2Array = chunk.GetComponentArray<T2>();

                for (var i = 0; i < entities.Count; i++)
                {
                    if (ShouldIncludeEntity(entities[i]))
                    {
                        yield return (entities[i], comp1Array[i], comp2Array[i]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 遍历匹配实体及其两个组件（引用返回，支持原地修改）
    /// </summary>
    public void EntitiesWithRef<T1, T2>(RefAction<T1, T2> action)
        where T1 : struct
        where T2 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            if (!archetype.HasComponent<T1>() || !archetype.HasComponent<T2>())
            {
                continue;
            }

            foreach (var chunk in archetype.Chunks)
            {
                var entities = chunk.GetAllEntities();
                var comp1Array = chunk.GetComponentArray<T1>();
                var comp2Array = chunk.GetComponentArray<T2>();

                for (var i = 0; i < entities.Count; i++)
                {
                    if (ShouldIncludeEntity(entities[i]))
                    {
                        action(entities[i], ref comp1Array[i], ref comp2Array[i]);
                    }
                }
            }
        }
    }

    #endregion

    #region 三组件迭代

    /// <summary>
    /// 遍历匹配实体及其三个组件（只读）
    /// </summary>
    public IEnumerable<(EntityId Entity, T1 Comp1, T2 Comp2, T3 Comp3)> EntitiesWith<T1, T2, T3>()
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            if (!archetype.HasComponent<T1>() || !archetype.HasComponent<T2>() || !archetype.HasComponent<T3>())
            {
                continue;
            }

            foreach (var chunk in archetype.Chunks)
            {
                var entities = chunk.GetAllEntities();
                var comp1Array = chunk.GetComponentArray<T1>();
                var comp2Array = chunk.GetComponentArray<T2>();
                var comp3Array = chunk.GetComponentArray<T3>();

                for (var i = 0; i < entities.Count; i++)
                {
                    if (ShouldIncludeEntity(entities[i]))
                    {
                        yield return (entities[i], comp1Array[i], comp2Array[i], comp3Array[i]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 遍历匹配实体及其三个组件（引用返回，支持原地修改）
    /// </summary>
    public void EntitiesWithRef<T1, T2, T3>(RefAction<T1, T2, T3> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            if (!archetype.HasComponent<T1>() || !archetype.HasComponent<T2>() || !archetype.HasComponent<T3>())
            {
                continue;
            }

            foreach (var chunk in archetype.Chunks)
            {
                var entities = chunk.GetAllEntities();
                var comp1Array = chunk.GetComponentArray<T1>();
                var comp2Array = chunk.GetComponentArray<T2>();
                var comp3Array = chunk.GetComponentArray<T3>();

                for (var i = 0; i < entities.Count; i++)
                {
                    if (ShouldIncludeEntity(entities[i]))
                    {
                        action(entities[i], ref comp1Array[i], ref comp2Array[i], ref comp3Array[i]);
                    }
                }
            }
        }
    }

    #endregion

    #region 四组件迭代

    /// <summary>
    /// 遍历匹配实体及其四个组件（只读）
    /// </summary>
    public IEnumerable<(EntityId Entity, T1 Comp1, T2 Comp2, T3 Comp3, T4 Comp4)> EntitiesWith<T1, T2, T3, T4>()
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            if (!archetype.HasComponent<T1>() || !archetype.HasComponent<T2>() ||
                !archetype.HasComponent<T3>() || !archetype.HasComponent<T4>())
            {
                continue;
            }

            foreach (var chunk in archetype.Chunks)
            {
                var entities = chunk.GetAllEntities();
                var comp1Array = chunk.GetComponentArray<T1>();
                var comp2Array = chunk.GetComponentArray<T2>();
                var comp3Array = chunk.GetComponentArray<T3>();
                var comp4Array = chunk.GetComponentArray<T4>();

                for (var i = 0; i < entities.Count; i++)
                {
                    if (ShouldIncludeEntity(entities[i]))
                    {
                        yield return (entities[i], comp1Array[i], comp2Array[i], comp3Array[i], comp4Array[i]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 遍历匹配实体及其四个组件（引用返回，支持原地修改）
    /// </summary>
    public void EntitiesWithRef<T1, T2, T3, T4>(RefAction<T1, T2, T3, T4> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            if (!archetype.HasComponent<T1>() || !archetype.HasComponent<T2>() ||
                !archetype.HasComponent<T3>() || !archetype.HasComponent<T4>())
            {
                continue;
            }

            foreach (var chunk in archetype.Chunks)
            {
                var entities = chunk.GetAllEntities();
                var comp1Array = chunk.GetComponentArray<T1>();
                var comp2Array = chunk.GetComponentArray<T2>();
                var comp3Array = chunk.GetComponentArray<T3>();
                var comp4Array = chunk.GetComponentArray<T4>();

                for (var i = 0; i < entities.Count; i++)
                {
                    if (ShouldIncludeEntity(entities[i]))
                    {
                        action(entities[i], ref comp1Array[i], ref comp2Array[i], ref comp3Array[i], ref comp4Array[i]);
                    }
                }
            }
        }
    }

    #endregion

    #region Chunk 级迭代

    /// <summary>
    /// 遍历所有匹配的 Chunk，用于需要 Chunk 级别批量操作的场景
    /// </summary>
    public IEnumerable<Chunk> Chunks()
    {
        foreach (var archetype in _matchingArchetypes)
        {
            foreach (var chunk in archetype.Chunks)
            {
                yield return chunk;
            }
        }
    }

    /// <summary>
    /// 遍历所有匹配的 Archetype，用于 Archetype 级别的批量操作
    /// </summary>
    public IEnumerable<ArchetypeEntity> Archetypes()
    {
        return _matchingArchetypes;
    }

    #endregion

    #region 私有方法

    private bool ShouldIncludeEntity(EntityId entityId)
    {
        if (_versionTracker == null || _changedSinceVersions == null || _changedSinceVersions.Count == 0)
        {
            return true;
        }

        foreach (var kvp in _changedSinceVersions)
        {
            if (_versionTracker.HasChanged(kvp.Key, entityId, kvp.Value))
            {
                return true;
            }
        }

        return false;
    }

    #endregion
}

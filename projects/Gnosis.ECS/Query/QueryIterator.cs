namespace Gnosis.ECS.Query;

/// <summary>
/// 查询迭代器，高效遍历匹配查询条件的实体和组件。
/// 直接从 Archetype 的 Chunk 中读取组件数据，避免间接寻址开销。
/// </summary>
public sealed class QueryIterator
{
    #region 字段

    private readonly List<Archetype> _matchingArchetypes;

    #endregion

    #region 构造函数

    public QueryIterator(IEnumerable<Archetype> archetypes)
    {
        _matchingArchetypes = new List<Archetype>(archetypes);
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
                yield return entityId;
            }
        }
    }

    #endregion

    #region 单组件迭代

    /// <summary>
    /// 遍历匹配实体及其单个组件
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
                    yield return (entities[i], compArray[i]);
                }
            }
        }
    }

    #endregion

    #region 双组件迭代

    /// <summary>
    /// 遍历匹配实体及其两个组件
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
                    yield return (entities[i], comp1Array[i], comp2Array[i]);
                }
            }
        }
    }

    #endregion

    #region 三组件迭代

    /// <summary>
    /// 遍历匹配实体及其三个组件
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
                    yield return (entities[i], comp1Array[i], comp2Array[i], comp3Array[i]);
                }
            }
        }
    }

    #endregion

    #region 四组件迭代

    /// <summary>
    /// 遍历匹配实体及其四个组件
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
                    yield return (entities[i], comp1Array[i], comp2Array[i], comp3Array[i], comp4Array[i]);
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
    /// 获取匹配的 Archetype 数量
    /// </summary>
    public int ArchetypeCount => _matchingArchetypes.Count;

    #endregion
}

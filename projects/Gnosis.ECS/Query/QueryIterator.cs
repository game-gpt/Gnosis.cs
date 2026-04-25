using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ArchetypeEntity = Gnosis.ECS.Archetype.Archetype;
using Gnosis.ECS.Archetype;
using Gnosis.ECS.Entity;
using Gnosis.ECS.Simd;

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
/// 优化策略：槽位快速访问、消除冗余 HasComponent 检查、无过滤器时跳过 ShouldIncludeEntity。
/// </summary>
public sealed class QueryIterator
{
    #region 字段

    private readonly List<ArchetypeEntity> _matchingArchetypes;
    private readonly ComponentVersionTracker? _versionTracker;
    private readonly Dictionary<Type, uint>? _changedSinceVersions;
    private readonly bool _hasChangeFilter;

    #endregion

    #region 属性

    /// <summary>
    /// 匹配的 Archetype 数量
    /// </summary>
    public int ArchetypeCount => _matchingArchetypes.Count;

    /// <summary>
    /// 是否启用变更过滤
    /// </summary>
    public bool HasChangeFilter => _hasChangeFilter;

    #endregion

    #region 构造函数

    public QueryIterator(IEnumerable<ArchetypeEntity> archetypes)
    {
        _matchingArchetypes = new List<ArchetypeEntity>(archetypes);
        _versionTracker = null;
        _changedSinceVersions = null;
        _hasChangeFilter = false;
    }

    public QueryIterator(
        IEnumerable<ArchetypeEntity> archetypes,
        ComponentVersionTracker versionTracker,
        Dictionary<Type, uint> changedSinceVersions)
    {
        _matchingArchetypes = new List<ArchetypeEntity>(archetypes);
        _versionTracker = versionTracker;
        _changedSinceVersions = new Dictionary<Type, uint>(changedSinceVersions);
        _hasChangeFilter = changedSinceVersions.Count > 0;
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
            var slot = archetype.GetComponentSlot<T1>();
            if (slot < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entities = chunk.GetAllEntities();
                var compArray = chunk.GetComponentArrayBySlot<T1>(slot);

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
    /// 遍历匹配实体及其单个组件（引用返回，支持原地修改，零 GC 分配）
    /// </summary>
    public void EntitiesWithRef<T1>(RefAction<T1> action) where T1 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            var slot = archetype.GetComponentSlot<T1>();
            if (slot < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var compArray = chunk.GetComponentArrayBySlot<T1>(slot);
                var count = chunk.Count;

                if (_hasChangeFilter)
                {
                    for (var i = 0; i < count; i++)
                    {
                        if (ShouldIncludeEntity(entityArray[i]))
                        {
                            action(entityArray[i], ref compArray[i]);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        action(entityArray[i], ref compArray[i]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 零分配遍历匹配实体及其单个组件（无变更过滤快速路径）
    /// </summary>
    public void ForEach<T1>(RefAction<T1> action) where T1 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            var slot = archetype.GetComponentSlot<T1>();
            if (slot < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var compArray = chunk.GetComponentArrayBySlot<T1>(slot);
                var count = chunk.Count;

                for (var i = 0; i < count; i++)
                {
                    action(entityArray[i], ref compArray[i]);
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
            var slot1 = archetype.GetComponentSlot<T1>();
            var slot2 = archetype.GetComponentSlot<T2>();
            if (slot1 < 0 || slot2 < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entities = chunk.GetAllEntities();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(slot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(slot2);

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
    /// 遍历匹配实体及其两个组件（引用返回，支持原地修改，零 GC 分配）
    /// </summary>
    public void EntitiesWithRef<T1, T2>(RefAction<T1, T2> action)
        where T1 : struct
        where T2 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            var slot1 = archetype.GetComponentSlot<T1>();
            var slot2 = archetype.GetComponentSlot<T2>();
            if (slot1 < 0 || slot2 < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(slot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(slot2);
                var count = chunk.Count;

                if (_hasChangeFilter)
                {
                    for (var i = 0; i < count; i++)
                    {
                        if (ShouldIncludeEntity(entityArray[i]))
                        {
                            action(entityArray[i], ref comp1Array[i], ref comp2Array[i]);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        action(entityArray[i], ref comp1Array[i], ref comp2Array[i]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 零分配遍历匹配实体及其两个组件（无变更过滤快速路径）
    /// </summary>
    public void ForEach<T1, T2>(RefAction<T1, T2> action)
        where T1 : struct
        where T2 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            var slot1 = archetype.GetComponentSlot<T1>();
            var slot2 = archetype.GetComponentSlot<T2>();
            if (slot1 < 0 || slot2 < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(slot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(slot2);
                var count = chunk.Count;

                for (var i = 0; i < count; i++)
                {
                    action(entityArray[i], ref comp1Array[i], ref comp2Array[i]);
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
            var slot1 = archetype.GetComponentSlot<T1>();
            var slot2 = archetype.GetComponentSlot<T2>();
            var slot3 = archetype.GetComponentSlot<T3>();
            if (slot1 < 0 || slot2 < 0 || slot3 < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entities = chunk.GetAllEntities();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(slot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(slot2);
                var comp3Array = chunk.GetComponentArrayBySlot<T3>(slot3);

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
    /// 遍历匹配实体及其三个组件（引用返回，支持原地修改，零 GC 分配）
    /// </summary>
    public void EntitiesWithRef<T1, T2, T3>(RefAction<T1, T2, T3> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            var slot1 = archetype.GetComponentSlot<T1>();
            var slot2 = archetype.GetComponentSlot<T2>();
            var slot3 = archetype.GetComponentSlot<T3>();
            if (slot1 < 0 || slot2 < 0 || slot3 < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(slot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(slot2);
                var comp3Array = chunk.GetComponentArrayBySlot<T3>(slot3);
                var count = chunk.Count;

                if (_hasChangeFilter)
                {
                    for (var i = 0; i < count; i++)
                    {
                        if (ShouldIncludeEntity(entityArray[i]))
                        {
                            action(entityArray[i], ref comp1Array[i], ref comp2Array[i], ref comp3Array[i]);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        action(entityArray[i], ref comp1Array[i], ref comp2Array[i], ref comp3Array[i]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 零分配遍历匹配实体及其三个组件（无变更过滤快速路径）
    /// </summary>
    public void ForEach<T1, T2, T3>(RefAction<T1, T2, T3> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            var slot1 = archetype.GetComponentSlot<T1>();
            var slot2 = archetype.GetComponentSlot<T2>();
            var slot3 = archetype.GetComponentSlot<T3>();
            if (slot1 < 0 || slot2 < 0 || slot3 < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(slot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(slot2);
                var comp3Array = chunk.GetComponentArrayBySlot<T3>(slot3);
                var count = chunk.Count;

                for (var i = 0; i < count; i++)
                {
                    action(entityArray[i], ref comp1Array[i], ref comp2Array[i], ref comp3Array[i]);
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
            var slot1 = archetype.GetComponentSlot<T1>();
            var slot2 = archetype.GetComponentSlot<T2>();
            var slot3 = archetype.GetComponentSlot<T3>();
            var slot4 = archetype.GetComponentSlot<T4>();
            if (slot1 < 0 || slot2 < 0 || slot3 < 0 || slot4 < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entities = chunk.GetAllEntities();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(slot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(slot2);
                var comp3Array = chunk.GetComponentArrayBySlot<T3>(slot3);
                var comp4Array = chunk.GetComponentArrayBySlot<T4>(slot4);

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
    /// 遍历匹配实体及其四个组件（引用返回，支持原地修改，零 GC 分配）
    /// </summary>
    public void EntitiesWithRef<T1, T2, T3, T4>(RefAction<T1, T2, T3, T4> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            var slot1 = archetype.GetComponentSlot<T1>();
            var slot2 = archetype.GetComponentSlot<T2>();
            var slot3 = archetype.GetComponentSlot<T3>();
            var slot4 = archetype.GetComponentSlot<T4>();
            if (slot1 < 0 || slot2 < 0 || slot3 < 0 || slot4 < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(slot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(slot2);
                var comp3Array = chunk.GetComponentArrayBySlot<T3>(slot3);
                var comp4Array = chunk.GetComponentArrayBySlot<T4>(slot4);
                var count = chunk.Count;

                if (_hasChangeFilter)
                {
                    for (var i = 0; i < count; i++)
                    {
                        if (ShouldIncludeEntity(entityArray[i]))
                        {
                            action(entityArray[i], ref comp1Array[i], ref comp2Array[i], ref comp3Array[i], ref comp4Array[i]);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        action(entityArray[i], ref comp1Array[i], ref comp2Array[i], ref comp3Array[i], ref comp4Array[i]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 零分配遍历匹配实体及其四个组件（无变更过滤快速路径）
    /// </summary>
    public void ForEach<T1, T2, T3, T4>(RefAction<T1, T2, T3, T4> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            var slot1 = archetype.GetComponentSlot<T1>();
            var slot2 = archetype.GetComponentSlot<T2>();
            var slot3 = archetype.GetComponentSlot<T3>();
            var slot4 = archetype.GetComponentSlot<T4>();
            if (slot1 < 0 || slot2 < 0 || slot3 < 0 || slot4 < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(slot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(slot2);
                var comp3Array = chunk.GetComponentArrayBySlot<T3>(slot3);
                var comp4Array = chunk.GetComponentArrayBySlot<T4>(slot4);
                var count = chunk.Count;

                for (var i = 0; i < count; i++)
                {
                    action(entityArray[i], ref comp1Array[i], ref comp2Array[i], ref comp3Array[i], ref comp4Array[i]);
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

    #region SIMD 批量遍历

    /// <summary>
    /// SIMD 加速的乘加遍历：对两个 float[] 组件执行 target[i] += operand[i] * scalar。
    /// 典型用途：Position += Velocity * deltaTime
    /// 直接操作 Chunk 内的组件数组，零 GC 分配，利用硬件向量指令。
    /// </summary>
    /// <typeparam name="TTarget">目标组件类型（如 Position）</typeparam>
    /// <typeparam name="TOperand">操作数组件类型（如 Velocity）</typeparam>
    /// <param name="targetSlot">目标组件在 Archetype 中的槽位（-1 表示自动查找）</param>
    /// <param name="operandSlot">操作数组件在 Archetype 中的槽位（-1 表示自动查找）</param>
    /// <param name="scalar">标量乘数（如 deltaTime）</param>
    /// <param name="fieldOffset">组件内 float 字段偏移（以 float 为单位）</param>
    /// <param name="fieldCount">每个组件的 float 字段数量（2 = Vector2, 3 = Vector3）</param>
    public void ForEachMultiplyAdd<TTarget, TOperand>(
        int targetSlot, int operandSlot, float scalar,
        int fieldOffset = 0, int fieldCount = 2)
        where TTarget : struct
        where TOperand : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            var tSlot = targetSlot >= 0 ? targetSlot : archetype.GetComponentSlot<TTarget>();
            var oSlot = operandSlot >= 0 ? operandSlot : archetype.GetComponentSlot<TOperand>();
            if (tSlot < 0 || oSlot < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var targetArray = chunk.GetComponentArrayBySlot<TTarget>(tSlot);
                var operandArray = chunk.GetComponentArrayBySlot<TOperand>(oSlot);
                var count = chunk.Count;

                ref var targetRef = ref Unsafe.As<TTarget, float>(ref targetArray[0]);
                ref var operandRef = ref Unsafe.As<TOperand, float>(ref operandArray[0]);

                var targetFloats = MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref targetRef, fieldOffset),
                    count * Unsafe.SizeOf<TTarget>() / sizeof(float) - fieldOffset);
                var operandFloats = MemoryMarshal.CreateSpan(
                    ref Unsafe.Add(ref operandRef, fieldOffset),
                    count * Unsafe.SizeOf<TOperand>() / sizeof(float) - fieldOffset);

                for (var e = 0; e < count; e++)
                {
                    var tStart = e * (Unsafe.SizeOf<TTarget>() / sizeof(float)) + fieldOffset;
                    var oStart = e * (Unsafe.SizeOf<TOperand>() / sizeof(float)) + fieldOffset;

                    for (var f = 0; f < fieldCount; f++)
                    {
                        Unsafe.Add(ref targetRef, tStart + f) +=
                            Unsafe.Add(ref operandRef, oStart + f) * scalar;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Chunk 级 SIMD 批量遍历，对每个 Chunk 调用回调。
    /// 回调直接接收组件数组和实体数量，可使用 SimdBatch 进行批量操作。
    /// 零 GC 分配，适用于自定义 SIMD 加速逻辑。
    /// </summary>
    public void ForEachChunk<T1, T2>(
        Action<EntityId[], T1[], T2[], int> action)
        where T1 : struct
        where T2 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            var slot1 = archetype.GetComponentSlot<T1>();
            var slot2 = archetype.GetComponentSlot<T2>();
            if (slot1 < 0 || slot2 < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(slot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(slot2);

                action(entityArray, comp1Array, comp2Array, chunk.Count);
            }
        }
    }

    /// <summary>
    /// Chunk 级 SIMD 批量遍历（三组件），对每个 Chunk 调用回调。
    /// </summary>
    public void ForEachChunk<T1, T2, T3>(
        Action<EntityId[], T1[], T2[], T3[], int> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        foreach (var archetype in _matchingArchetypes)
        {
            var slot1 = archetype.GetComponentSlot<T1>();
            var slot2 = archetype.GetComponentSlot<T2>();
            var slot3 = archetype.GetComponentSlot<T3>();
            if (slot1 < 0 || slot2 < 0 || slot3 < 0) continue;

            foreach (var chunk in archetype.Chunks)
            {
                var entityArray = chunk.GetEntityArray();
                var comp1Array = chunk.GetComponentArrayBySlot<T1>(slot1);
                var comp2Array = chunk.GetComponentArrayBySlot<T2>(slot2);
                var comp3Array = chunk.GetComponentArrayBySlot<T3>(slot3);

                action(entityArray, comp1Array, comp2Array, comp3Array, chunk.Count);
            }
        }
    }

    /// <summary>
    /// 获取匹配的实体总数（不实际遍历实体，仅统计 Archetype 中的实体数）
    /// </summary>
    public int GetEntityCount()
    {
        var count = 0;
        foreach (var archetype in _matchingArchetypes)
        {
            count += archetype.EntityCount;
        }

        return count;
    }

    #endregion

    #region 私有方法

    private bool ShouldIncludeEntity(EntityId entityId)
    {
        if (!_hasChangeFilter)
        {
            return true;
        }

        foreach (var kvp in _changedSinceVersions!)
        {
            if (_versionTracker!.HasChanged(kvp.Key, entityId, kvp.Value))
            {
                return true;
            }
        }

        return false;
    }

    #endregion
}

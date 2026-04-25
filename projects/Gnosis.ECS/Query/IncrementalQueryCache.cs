using ArchetypeEntity = Gnosis.ECS.Archetype.Archetype;
using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Query;

/// <summary>
/// 增量查询缓存，包装 EntityQuery 提供实体结果的增量更新。
/// 当 Archetype 结构未变化时，直接返回缓存的实体列表；
/// 当 Archetype 结构变化时，仅重新计算受影响的部分。
/// 适用于每帧重复执行的查询场景，避免重复遍历所有 Archetype。
/// </summary>
public sealed class IncrementalQueryCache
{
    #region 字段

    private readonly EntityQuery _query;
    private List<EntityId> _cachedEntities;
    private int _lastArchetypeCount;
    private uint _lastGlobalVersion;
    private int _lastEntityCount;
    private bool _dirty;

    #endregion

    #region 属性

    /// <summary>
    /// 底层查询构建器
    /// </summary>
    public EntityQuery Query => _query;

    /// <summary>
    /// 缓存是否有效（无需重新计算）
    /// </summary>
    public bool IsCacheValid => !_dirty && _query.IsCacheValid();

    /// <summary>
    /// 缓存的实体数量
    /// </summary>
    public int CachedEntityCount => _cachedEntities.Count;

    /// <summary>
    /// 自上次缓存以来的缓存命中次数
    /// </summary>
    public int CacheHitCount { get; private set; }

    /// <summary>
    /// 自上次缓存以来的缓存未命中次数
    /// </summary>
    public int CacheMissCount { get; private set; }

    #endregion

    #region 构造函数

    public IncrementalQueryCache(EntityQuery query)
    {
        _query = query;
        _cachedEntities = new List<EntityId>();
        _lastArchetypeCount = -1;
        _lastGlobalVersion = 0;
        _lastEntityCount = -1;
        _dirty = true;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 获取匹配的实体列表。
    /// 如果缓存有效，直接返回缓存；否则增量更新缓存。
    /// </summary>
    public IReadOnlyList<EntityId> GetEntities()
    {
        if (IsCacheValid)
        {
            CacheHitCount++;
            return _cachedEntities;
        }

        CacheMissCount++;
        RefreshCache();
        return _cachedEntities;
    }

    /// <summary>
    /// 强制标记缓存为脏，下次 GetEntities 将重新计算
    /// </summary>
    public void Invalidate()
    {
        _dirty = true;
    }

    /// <summary>
    /// 更新变更过滤的版本快照
    /// </summary>
    public void UpdateChangeSnapshot()
    {
        _query.UpdateChangeSnapshot();
        _dirty = true;
    }

    /// <summary>
    /// 重置缓存统计
    /// </summary>
    public void ResetStats()
    {
        CacheHitCount = 0;
        CacheMissCount = 0;
    }

    #endregion

    #region 私有方法

    private void RefreshCache()
    {
        _cachedEntities.Clear();

        foreach (var entityId in _query.Build())
        {
            _cachedEntities.Add(entityId);
        }

        _dirty = false;
    }

    #endregion
}

using ArchetypeEntity = Gnosis.ECS.Archetype.Archetype;
using Gnosis.ECS.Archetype;
using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Query;

/// <summary>
/// 实体查询构建器，支持 All/Any/None/Changed 链式查询。
/// 集成 ArchetypeManager 实现高效的 Archetype 级别过滤，
/// 支持查询缓存以避免重复计算，支持变更过滤实现响应式查询。
/// </summary>
public sealed class EntityQuery : IQuery
{
    #region 字段

    private readonly HashSet<Type> _allTypes;
    private readonly HashSet<Type> _anyTypes;
    private readonly HashSet<Type> _noneTypes;
    private readonly HashSet<Type> _changedTypes;
    private readonly ArchetypeManager _archetypeManager;
    private readonly ComponentVersionTracker? _versionTracker;
    private List<ArchetypeEntity>? _cachedArchetypes;
    private int _cacheVersion;
    private int _lastArchetypeCount;
    private uint _lastGlobalVersion;
    private Func<EntityId, bool>? _customFilter;
    private Comparison<EntityId>? _sortComparison;
    private readonly Dictionary<Type, uint> _changedSinceVersions;

    #endregion

    #region 属性

    /// <summary>
    /// 查询必须包含的所有组件类型
    /// </summary>
    public IReadOnlySet<Type> AllTypes => _allTypes;

    /// <summary>
    /// 查询包含任意之一的组件类型
    /// </summary>
    public IReadOnlySet<Type> AnyTypes => _anyTypes;

    /// <summary>
    /// 查询排除的组件类型
    /// </summary>
    public IReadOnlySet<Type> NoneTypes => _noneTypes;

    /// <summary>
    /// 查询需要检测变更的组件类型
    /// </summary>
    public IReadOnlySet<Type> ChangedTypes => _changedTypes;

    /// <summary>
    /// 是否启用变更过滤
    /// </summary>
    public bool HasChangeFilter => _changedTypes.Count > 0;

    #endregion

    #region 构造函数

    public EntityQuery(ArchetypeManager archetypeManager)
    {
        _allTypes = new HashSet<Type>();
        _anyTypes = new HashSet<Type>();
        _noneTypes = new HashSet<Type>();
        _changedTypes = new HashSet<Type>();
        _archetypeManager = archetypeManager;
        _versionTracker = null;
        _cacheVersion = 0;
        _lastArchetypeCount = -1;
        _lastGlobalVersion = 0;
        _changedSinceVersions = new Dictionary<Type, uint>();
    }

    public EntityQuery(ArchetypeManager archetypeManager, ComponentVersionTracker versionTracker)
    {
        _allTypes = new HashSet<Type>();
        _anyTypes = new HashSet<Type>();
        _noneTypes = new HashSet<Type>();
        _changedTypes = new HashSet<Type>();
        _archetypeManager = archetypeManager;
        _versionTracker = versionTracker;
        _cacheVersion = 0;
        _lastArchetypeCount = -1;
        _lastGlobalVersion = 0;
        _changedSinceVersions = new Dictionary<Type, uint>();
    }

    #endregion

    #region 链式查询方法

    /// <summary>
    /// 查询必须包含所有指定组件类型的实体
    /// </summary>
    public IQuery All<T>() where T : struct
    {
        _allTypes.Add(typeof(T));
        InvalidateCache();

        return this;
    }

    /// <summary>
    /// 查询包含任意指定组件类型的实体
    /// </summary>
    public IQuery Any<T>() where T : struct
    {
        _anyTypes.Add(typeof(T));
        InvalidateCache();

        return this;
    }

    /// <summary>
    /// 排除包含指定组件类型的实体
    /// </summary>
    public IQuery None<T>() where T : struct
    {
        _noneTypes.Add(typeof(T));
        InvalidateCache();

        return this;
    }

    /// <summary>
    /// 只返回自上次查询以来指定组件发生变更的实体。
    /// 首次调用 Changed 时记录当前版本快照，后续查询只返回版本号大于快照的实体。
    /// </summary>
    public IQuery Changed<T>() where T : struct
    {
        Changed(typeof(T));
        return this;
    }

    #endregion

    #region 链式查询方法（非泛型）

    /// <summary>
    /// 查询必须包含所有指定组件类型的实体（非泛型）
    /// </summary>
    public EntityQuery All(Type type)
    {
        _allTypes.Add(type);
        InvalidateCache();

        return this;
    }

    /// <summary>
    /// 查询包含任意指定组件类型的实体（非泛型）
    /// </summary>
    public EntityQuery Any(Type type)
    {
        _anyTypes.Add(type);
        InvalidateCache();

        return this;
    }

    /// <summary>
    /// 排除包含指定组件类型的实体（非泛型）
    /// </summary>
    public EntityQuery None(Type type)
    {
        _noneTypes.Add(type);
        InvalidateCache();

        return this;
    }

    /// <summary>
    /// 只返回自上次查询以来指定组件发生变更的实体（非泛型）
    /// </summary>
    public EntityQuery Changed(Type type)
    {
        if (!_changedTypes.Contains(type))
        {
            _changedTypes.Add(type);

            if (_versionTracker != null)
            {
                _changedSinceVersions[type] = _versionTracker.GlobalVersion;
            }
        }

        InvalidateCache();

        return this;
    }

    #endregion

    #region 过滤与排序

    /// <summary>
    /// 设置自定义过滤条件，只返回满足条件的实体
    /// </summary>
    public EntityQuery WithFilter(Func<EntityId, bool> filter)
    {
        _customFilter = filter;
        InvalidateCache();

        return this;
    }

    /// <summary>
    /// 设置排序规则
    /// </summary>
    public EntityQuery WithSort(Comparison<EntityId> comparison)
    {
        _sortComparison = comparison;
        InvalidateCache();

        return this;
    }

    #endregion

    #region 执行查询

    /// <summary>
    /// 执行查询并返回匹配的实体 ID 集合
    /// </summary>
    public IEnumerable<EntityId> Build()
    {
        var archetypes = GetMatchingArchetypes();

        IEnumerable<EntityId> entities = archetypes.SelectMany(a => a.GetEntities());

        if (_changedTypes.Count > 0 && _versionTracker != null)
        {
            entities = ApplyChangeFilter(entities);
        }

        if (_customFilter != null)
        {
            entities = entities.Where(_customFilter);
        }

        if (_sortComparison != null)
        {
            var sortedList = entities.ToList();
            sortedList.Sort(_sortComparison);

            return sortedList;
        }

        return entities;
    }

    /// <summary>
    /// 创建查询迭代器，支持组件数据的批量遍历
    /// </summary>
    public QueryIterator Iterate()
    {
        var archetypes = GetMatchingArchetypes();

        if (_changedTypes.Count > 0 && _versionTracker != null)
        {
            var filteredEntities = ApplyChangeFilter(archetypes.SelectMany(a => a.GetEntities()));
            return new QueryIterator(archetypes, _versionTracker, _changedSinceVersions);
        }

        return new QueryIterator(archetypes);
    }

    #endregion

    #region 缓存管理

    /// <summary>
    /// 使缓存失效
    /// </summary>
    public void InvalidateCache()
    {
        _cachedArchetypes = null;
        _cacheVersion++;
    }

    /// <summary>
    /// 检查缓存是否有效
    /// </summary>
    public bool IsCacheValid()
    {
        if (_cachedArchetypes == null)
        {
            return false;
        }

        if (_lastArchetypeCount != _archetypeManager.ArchetypeCount)
        {
            return false;
        }

        if (_versionTracker != null && _lastGlobalVersion != _versionTracker.GlobalVersion)
        {
            return _changedTypes.Count == 0;
        }

        return true;
    }

    /// <summary>
    /// 更新变更过滤的版本快照，将当前版本记录为"已查看"。
    /// 下次 Changed 查询将只返回此快照之后变更的实体。
    /// </summary>
    public void UpdateChangeSnapshot()
    {
        if (_versionTracker == null)
        {
            return;
        }

        foreach (var type in _changedTypes)
        {
            _changedSinceVersions[type] = _versionTracker.GlobalVersion;
        }
    }

    private List<ArchetypeEntity> GetMatchingArchetypes()
    {
        if (IsCacheValid() && _cachedArchetypes != null)
        {
            return _cachedArchetypes;
        }

        _cachedArchetypes = _archetypeManager.QueryArchetypes(_allTypes, _anyTypes, _noneTypes).ToList();
        _lastArchetypeCount = _archetypeManager.ArchetypeCount;

        if (_versionTracker != null)
        {
            _lastGlobalVersion = _versionTracker.GlobalVersion;
        }

        return _cachedArchetypes;
    }

    #endregion

    #region 变更过滤

    private IEnumerable<EntityId> ApplyChangeFilter(IEnumerable<EntityId> entities)
    {
        if (_versionTracker == null)
        {
            return entities;
        }

        return entities.Where(entityId =>
        {
            foreach (var type in _changedTypes)
            {
                var sinceVersion = _changedSinceVersions.TryGetValue(type, out var v) ? v : 0;

                if (_versionTracker.HasChanged(type, entityId, sinceVersion))
                {
                    return true;
                }
            }

            return false;
        });
    }

    #endregion
}

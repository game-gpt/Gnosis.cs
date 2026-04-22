using Gnosis.ECS.Archetype;
using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Query;

/// <summary>
/// 实体查询构建器，支持 All/Any/None 链式查询。
/// 集成 ArchetypeManager 实现高效的 Archetype 级别过滤，
/// 支持查询缓存以避免重复计算。
/// </summary>
public sealed class EntityQuery : IQuery
{
    #region 字段

    private readonly HashSet<Type> _allTypes;
    private readonly HashSet<Type> _anyTypes;
    private readonly HashSet<Type> _noneTypes;
    private readonly ArchetypeManager _archetypeManager;
    private List<Archetype.Archetype>? _cachedArchetypes;
    private int _cacheVersion;
    private int _lastArchetypeCount;
    private Func<EntityId, bool>? _changeFilter;
    private Comparison<EntityId>? _sortComparison;

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

    #endregion

    #region 构造函数

    public EntityQuery(ArchetypeManager archetypeManager)
    {
        _allTypes = new HashSet<Type>();
        _anyTypes = new HashSet<Type>();
        _noneTypes = new HashSet<Type>();
        _archetypeManager = archetypeManager;
        _cacheVersion = 0;
        _lastArchetypeCount = -1;
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

    #endregion

    #region 过滤与排序

    /// <summary>
    /// 设置变更过滤，只返回满足条件的实体
    /// </summary>
    public EntityQuery WithFilter(Func<EntityId, bool> filter)
    {
        _changeFilter = filter;
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

        if (_changeFilter != null)
        {
            entities = entities.Where(_changeFilter);
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
        return _cachedArchetypes != null && _lastArchetypeCount == _archetypeManager.ArchetypeCount;
    }

    private List<Archetype> GetMatchingArchetypes()
    {
        if (IsCacheValid() && _cachedArchetypes != null)
        {
            return _cachedArchetypes;
        }

        _cachedArchetypes = _archetypeManager.QueryArchetypes(_allTypes, _anyTypes, _noneTypes).ToList();
        _lastArchetypeCount = _archetypeManager.ArchetypeCount;

        return _cachedArchetypes;
    }

    #endregion
}

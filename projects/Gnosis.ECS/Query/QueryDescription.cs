using ArchetypeEntity = Gnosis.ECS.Archetype.Archetype;

namespace Gnosis.ECS.Query;

/// <summary>
/// 可复用的查询描述符，定义查询条件的不可变快照。
/// 可在多个查询之间共享，支持编译时优化和缓存。
/// </summary>
public sealed class QueryDescription
{
    #region 属性

    /// <summary>
    /// 查询必须包含的所有组件类型
    /// </summary>
    public IReadOnlySet<Type> AllTypes { get; }

    /// <summary>
    /// 查询包含任意之一的组件类型
    /// </summary>
    public IReadOnlySet<Type> AnyTypes { get; }

    /// <summary>
    /// 查询排除的组件类型
    /// </summary>
    public IReadOnlySet<Type> NoneTypes { get; }

    /// <summary>
    /// 查询需要检测变更的组件类型
    /// </summary>
    public IReadOnlySet<Type> ChangedTypes { get; }

    /// <summary>
    /// 描述符的哈希值，用于快速比较和缓存键
    /// </summary>
    public int HashCode { get; }

    #endregion

    #region 构造函数

    public QueryDescription(
        IEnumerable<Type>? allTypes = null,
        IEnumerable<Type>? anyTypes = null,
        IEnumerable<Type>? noneTypes = null,
        IEnumerable<Type>? changedTypes = null)
    {
        AllTypes = allTypes != null ? new HashSet<Type>(allTypes) : new HashSet<Type>();
        AnyTypes = anyTypes != null ? new HashSet<Type>(anyTypes) : new HashSet<Type>();
        NoneTypes = noneTypes != null ? new HashSet<Type>(noneTypes) : new HashSet<Type>();
        ChangedTypes = changedTypes != null ? new HashSet<Type>(changedTypes) : new HashSet<Type>();
        HashCode = ComputeHash();
    }

    #endregion

    #region 构建器

    /// <summary>
    /// 从 EntityQuery 创建查询描述符
    /// </summary>
    public static QueryDescription FromQuery(EntityQuery query)
    {
        return new QueryDescription(
            query.AllTypes,
            query.AnyTypes,
            query.NoneTypes,
            query.ChangedTypes);
    }

    /// <summary>
    /// 创建查询描述符构建器
    /// </summary>
    public static QueryDescriptionBuilder Builder()
    {
        return new QueryDescriptionBuilder();
    }

    #endregion

    #region 匹配判断

    /// <summary>
    /// 检查给定 Archetype 是否匹配此查询描述符
    /// </summary>
    public bool Matches(ArchetypeEntity archetype)
    {
        if (AllTypes.Count > 0 && !AllTypes.IsSubsetOf(archetype.ComponentTypes))
        {
            return false;
        }

        if (AnyTypes.Count > 0 && !AnyTypes.Overlaps(archetype.ComponentTypes))
        {
            return false;
        }

        if (NoneTypes.Count > 0 && NoneTypes.Overlaps(archetype.ComponentTypes))
        {
            return false;
        }

        return true;
    }

    #endregion

    #region 相等性

    public override bool Equals(object? obj)
    {
        if (obj is not QueryDescription other)
        {
            return false;
        }

        return AllTypes.SetEquals(other.AllTypes) &&
               AnyTypes.SetEquals(other.AnyTypes) &&
               NoneTypes.SetEquals(other.NoneTypes) &&
               ChangedTypes.SetEquals(other.ChangedTypes);
    }

    public override int GetHashCode()
    {
        return HashCode;
    }

    #endregion

    #region 私有方法

    private int ComputeHash()
    {
        var hash = new HashCode();

        foreach (var type in AllTypes.OrderBy(t => t.FullName))
        {
            hash.Add(type);
        }

        foreach (var type in AnyTypes.OrderBy(t => t.FullName))
        {
            hash.Add(type);
        }

        foreach (var type in NoneTypes.OrderBy(t => t.FullName))
        {
            hash.Add(type);
        }

        foreach (var type in ChangedTypes.OrderBy(t => t.FullName))
        {
            hash.Add(type);
        }

        return hash.ToHashCode();
    }

    #endregion
}

/// <summary>
/// 查询描述符构建器，支持链式调用
/// </summary>
public sealed class QueryDescriptionBuilder
{
    #region 字段

    private readonly HashSet<Type> _allTypes = new();
    private readonly HashSet<Type> _anyTypes = new();
    private readonly HashSet<Type> _noneTypes = new();
    private readonly HashSet<Type> _changedTypes = new();

    #endregion

    #region 链式方法

    /// <summary>
    /// 添加必须包含的组件类型
    /// </summary>
    public QueryDescriptionBuilder All<T>() where T : struct
    {
        _allTypes.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// 添加必须包含的组件类型（非泛型）
    /// </summary>
    public QueryDescriptionBuilder All(Type type)
    {
        _allTypes.Add(type);
        return this;
    }

    /// <summary>
    /// 添加任意包含的组件类型
    /// </summary>
    public QueryDescriptionBuilder Any<T>() where T : struct
    {
        _anyTypes.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// 添加任意包含的组件类型（非泛型）
    /// </summary>
    public QueryDescriptionBuilder Any(Type type)
    {
        _anyTypes.Add(type);
        return this;
    }

    /// <summary>
    /// 添加排除的组件类型
    /// </summary>
    public QueryDescriptionBuilder None<T>() where T : struct
    {
        _noneTypes.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// 添加排除的组件类型（非泛型）
    /// </summary>
    public QueryDescriptionBuilder None(Type type)
    {
        _noneTypes.Add(type);
        return this;
    }

    /// <summary>
    /// 添加变更检测的组件类型
    /// </summary>
    public QueryDescriptionBuilder Changed<T>() where T : struct
    {
        _changedTypes.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// 添加变更检测的组件类型（非泛型）
    /// </summary>
    public QueryDescriptionBuilder Changed(Type type)
    {
        _changedTypes.Add(type);
        return this;
    }

    /// <summary>
    /// 构建不可变的查询描述符
    /// </summary>
    public QueryDescription Build()
    {
        return new QueryDescription(_allTypes, _anyTypes, _noneTypes, _changedTypes);
    }

    #endregion
}

using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Query;

/// <summary>
/// 组件变更追踪器，记录每个实体每个组件类型的修改版本号。
/// 用于实现变更过滤查询（Changed&lt;T&gt;），避免轮询检测。
/// </summary>
public sealed class ComponentVersionTracker
{
    #region 字段

    private readonly Dictionary<Type, Dictionary<EntityId, uint>> _versions;
    private uint _globalVersion;

    #endregion

    #region 属性

    /// <summary>
    /// 全局版本号，每次任何组件变更时递增
    /// </summary>
    public uint GlobalVersion => _globalVersion;

    #endregion

    #region 构造函数

    public ComponentVersionTracker()
    {
        _versions = new Dictionary<Type, Dictionary<EntityId, uint>>();
        _globalVersion = 0;
    }

    #endregion

    #region 版本标记

    /// <summary>
    /// 标记指定实体的组件已变更，递增其版本号
    /// </summary>
    public void MarkChanged<T>(EntityId entityId) where T : struct
    {
        var type = typeof(T);

        if (!_versions.TryGetValue(type, out var entityVersions))
        {
            entityVersions = new Dictionary<EntityId, uint>();
            _versions[type] = entityVersions;
        }

        _globalVersion++;
        entityVersions[entityId] = _globalVersion;
    }

    /// <summary>
    /// 标记指定实体的组件已变更（非泛型版本）
    /// </summary>
    public void MarkChanged(Type componentType, EntityId entityId)
    {
        if (!_versions.TryGetValue(componentType, out var entityVersions))
        {
            entityVersions = new Dictionary<EntityId, uint>();
            _versions[componentType] = entityVersions;
        }

        _globalVersion++;
        entityVersions[entityId] = _globalVersion;
    }

    /// <summary>
    /// 批量标记组件变更（用于批量操作后的标记）
    /// </summary>
    public void MarkChangedBatch<T>(IEnumerable<EntityId> entityIds) where T : struct
    {
        var type = typeof(T);

        if (!_versions.TryGetValue(type, out var entityVersions))
        {
            entityVersions = new Dictionary<EntityId, uint>();
            _versions[type] = entityVersions;
        }

        foreach (var entityId in entityIds)
        {
            _globalVersion++;
            entityVersions[entityId] = _globalVersion;
        }
    }

    #endregion

    #region 版本查询

    /// <summary>
    /// 获取指定实体组件的当前版本号，未追踪返回 0
    /// </summary>
    public uint GetVersion<T>(EntityId entityId) where T : struct
    {
        if (_versions.TryGetValue(typeof(T), out var entityVersions))
        {
            return entityVersions.GetValueOrDefault(entityId, 0);
        }

        return 0;
    }

    /// <summary>
    /// 获取指定实体组件的当前版本号（非泛型版本）
    /// </summary>
    public uint GetVersion(Type componentType, EntityId entityId)
    {
        if (_versions.TryGetValue(componentType, out var entityVersions))
        {
            return entityVersions.GetValueOrDefault(entityId, 0);
        }

        return 0;
    }

    /// <summary>
    /// 检查指定实体的组件自给定版本后是否发生变更
    /// </summary>
    public bool HasChanged<T>(EntityId entityId, uint sinceVersion) where T : struct
    {
        var currentVersion = GetVersion<T>(entityId);
        return currentVersion > sinceVersion;
    }

    /// <summary>
    /// 检查指定实体的组件自给定版本后是否发生变更（非泛型版本）
    /// </summary>
    public bool HasChanged(Type componentType, EntityId entityId, uint sinceVersion)
    {
        var currentVersion = GetVersion(componentType, entityId);
        return currentVersion > sinceVersion;
    }

    #endregion

    #region 实体管理

    /// <summary>
    /// 实体销毁时清理其所有版本记录
    /// </summary>
    public void OnEntityDestroyed(EntityId entityId)
    {
        foreach (var entityVersions in _versions.Values)
        {
            entityVersions.Remove(entityId);
        }
    }

    /// <summary>
    /// 清除所有版本记录
    /// </summary>
    public void Clear()
    {
        _versions.Clear();
        _globalVersion = 0;
    }

    #endregion

    #region 快照

    /// <summary>
    /// 获取指定组件类型的当前全局版本快照，用于后续变更检测
    /// </summary>
    public ChangeSnapshot CreateSnapshot()
    {
        return new ChangeSnapshot(_globalVersion);
    }

    /// <summary>
    /// 获取指定组件类型在指定实体上的版本快照
    /// </summary>
    public ChangeSnapshot CreateSnapshot<T>(EntityId entityId) where T : struct
    {
        return new ChangeSnapshot(GetVersion<T>(entityId));
    }

    #endregion
}

/// <summary>
/// 变更快照，记录某一时刻的版本号，用于后续对比检测变更
/// </summary>
public readonly record struct ChangeSnapshot(uint Version)
{
    /// <summary>
    /// 空快照，版本号为 0
    /// </summary>
    public static readonly ChangeSnapshot Empty = new(0);
}

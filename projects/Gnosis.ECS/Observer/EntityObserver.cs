using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Observer;

/// <summary>
/// 实体观察者，监听组件的添加、修改和删除事件。
/// 支持泛型和非泛型的回调注册与通知触发。
/// </summary>
public sealed class EntityObserver
{
    #region 字段

    private readonly Dictionary<Type, List<Action<EntityId>>> _onAdded = new();
    private readonly Dictionary<Type, List<Action<EntityId>>> _onRemoved = new();
    private readonly Dictionary<Type, List<Action<EntityId>>> _onChanged = new();

    #endregion

    #region 公开方法 - 注册观察（泛型）

    /// <summary>
    /// 注册组件添加事件的观察回调
    /// </summary>
    public void OnAdded<T>(Action<EntityId> callback) where T : struct
    {
        OnAdded(typeof(T), callback);
    }

    /// <summary>
    /// 注册组件移除事件的观察回调
    /// </summary>
    public void OnRemoved<T>(Action<EntityId> callback) where T : struct
    {
        OnRemoved(typeof(T), callback);
    }

    /// <summary>
    /// 注册组件变更事件的观察回调
    /// </summary>
    public void OnChanged<T>(Action<EntityId> callback) where T : struct
    {
        OnChanged(typeof(T), callback);
    }

    #endregion

    #region 公开方法 - 注册观察（非泛型）

    /// <summary>
    /// 注册组件添加事件的观察回调（非泛型）
    /// </summary>
    public void OnAdded(Type componentType, Action<EntityId> callback)
    {
        if (!_onAdded.TryGetValue(componentType, out var list))
        {
            list = [];
            _onAdded[componentType] = list;
        }

        list.Add(callback);
    }

    /// <summary>
    /// 注册组件移除事件的观察回调（非泛型）
    /// </summary>
    public void OnRemoved(Type componentType, Action<EntityId> callback)
    {
        if (!_onRemoved.TryGetValue(componentType, out var list))
        {
            list = [];
            _onRemoved[componentType] = list;
        }

        list.Add(callback);
    }

    /// <summary>
    /// 注册组件变更事件的观察回调（非泛型）
    /// </summary>
    public void OnChanged(Type componentType, Action<EntityId> callback)
    {
        if (!_onChanged.TryGetValue(componentType, out var list))
        {
            list = [];
            _onChanged[componentType] = list;
        }

        list.Add(callback);
    }

    #endregion

    #region 公开方法 - 触发通知（泛型）

    /// <summary>
    /// 触发组件添加通知
    /// </summary>
    public void NotifyAdded<T>(EntityId entityId) where T : struct
    {
        NotifyAdded(typeof(T), entityId);
    }

    /// <summary>
    /// 触发组件移除通知
    /// </summary>
    public void NotifyRemoved<T>(EntityId entityId) where T : struct
    {
        NotifyRemoved(typeof(T), entityId);
    }

    /// <summary>
    /// 触发组件变更通知
    /// </summary>
    public void NotifyChanged<T>(EntityId entityId) where T : struct
    {
        NotifyChanged(typeof(T), entityId);
    }

    #endregion

    #region 公开方法 - 触发通知（非泛型）

    /// <summary>
    /// 触发组件添加通知（非泛型）
    /// </summary>
    public void NotifyAdded(Type componentType, EntityId entityId)
    {
        if (_onAdded.TryGetValue(componentType, out var list))
        {
            foreach (var callback in list)
            {
                callback(entityId);
            }
        }
    }

    /// <summary>
    /// 触发组件移除通知（非泛型）
    /// </summary>
    public void NotifyRemoved(Type componentType, EntityId entityId)
    {
        if (_onRemoved.TryGetValue(componentType, out var list))
        {
            foreach (var callback in list)
            {
                callback(entityId);
            }
        }
    }

    /// <summary>
    /// 触发组件变更通知（非泛型）
    /// </summary>
    public void NotifyChanged(Type componentType, EntityId entityId)
    {
        if (_onChanged.TryGetValue(componentType, out var list))
        {
            foreach (var callback in list)
            {
                callback(entityId);
            }
        }
    }

    #endregion

    #region 公开方法 - 清理

    /// <summary>
    /// 清除所有观察回调
    /// </summary>
    public void ClearAll()
    {
        _onAdded.Clear();
        _onRemoved.Clear();
        _onChanged.Clear();
    }

    #endregion
}

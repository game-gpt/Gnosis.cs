using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Observer;

/// <summary>
/// 实体观察者，监听组件的添加、修改和删除事件
/// </summary>
public sealed class EntityObserver
{
    #region 字段

    private readonly Dictionary<Type, List<Action<EntityId>>> _onAdded = new();
    private readonly Dictionary<Type, List<Action<EntityId>>> _onRemoved = new();
    private readonly Dictionary<Type, List<Action<EntityId>>> _onChanged = new();

    #endregion

    #region 公开方法 - 注册观察

    public void OnAdded<T>(Action<EntityId> callback) where T : struct
    {
        var type = typeof(T);

        if (!_onAdded.TryGetValue(type, out var list))
        {
            list = [];
            _onAdded[type] = list;
        }

        list.Add(callback);
    }

    public void OnRemoved<T>(Action<EntityId> callback) where T : struct
    {
        var type = typeof(T);

        if (!_onRemoved.TryGetValue(type, out var list))
        {
            list = [];
            _onRemoved[type] = list;
        }

        list.Add(callback);
    }

    public void OnChanged<T>(Action<EntityId> callback) where T : struct
    {
        var type = typeof(T);

        if (!_onChanged.TryGetValue(type, out var list))
        {
            list = [];
            _onChanged[type] = list;
        }

        list.Add(callback);
    }

    #endregion

    #region 公开方法 - 触发通知

    public void NotifyAdded<T>(EntityId entityId) where T : struct
    {
        if (_onAdded.TryGetValue(typeof(T), out var list))
        {
            foreach (var callback in list)
            {
                callback(entityId);
            }
        }
    }

    public void NotifyRemoved<T>(EntityId entityId) where T : struct
    {
        if (_onRemoved.TryGetValue(typeof(T), out var list))
        {
            foreach (var callback in list)
            {
                callback(entityId);
            }
        }
    }

    public void NotifyChanged<T>(EntityId entityId) where T : struct
    {
        if (_onChanged.TryGetValue(typeof(T), out var list))
        {
            foreach (var callback in list)
            {
                callback(entityId);
            }
        }
    }

    #endregion

    #region 公开方法 - 清理

    public void ClearAll()
    {
        _onAdded.Clear();
        _onRemoved.Clear();
        _onChanged.Clear();
    }

    #endregion
}

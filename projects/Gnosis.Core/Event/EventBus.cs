using System.Runtime.CompilerServices;

namespace Gnosis.Core.Event;

/// <summary>
/// 事件总线，支持类型安全的事件发布与订阅，零分配设计
/// </summary>
public sealed class EventBus
{
    #region 字段

    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lock = new();

    #endregion

    #region 订阅

    /// <summary>
    /// 订阅指定类型的事件
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        lock (_lock)
        {
            var eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out var list))
            {
                list = new List<Delegate>();
                _handlers[eventType] = list;
            }

            list.Add(handler);
        }
    }

    /// <summary>
    /// 取消订阅指定类型的事件
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler is null)
        {
            return;
        }

        lock (_lock)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var list))
            {
                list.Remove(handler);
            }
        }
    }

    #endregion

    #region 发布

    /// <summary>
    /// 发布事件，同步调用所有订阅者
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Publish<TEvent>(TEvent evt)
    {
        List<Delegate>? handlers;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var list))
            {
                return;
            }

            handlers = list;
        }

        for (var i = 0; i < handlers.Count; i++)
        {
            if (handlers[i] is Action<TEvent> handler)
            {
                handler(evt);
            }
        }
    }

    #endregion

    #region 清理

    /// <summary>
    /// 清除所有订阅
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        lock (_lock)
        {
            _handlers.Clear();
        }
    }

    /// <summary>
    /// 清除指定事件类型的所有订阅
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear<TEvent>()
    {
        lock (_lock)
        {
            _handlers.Remove(typeof(TEvent));
        }
    }

    #endregion
}

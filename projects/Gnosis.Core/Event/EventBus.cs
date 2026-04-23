using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Gnosis.Core.Event;

/// <summary>
/// 统一的事件总线，支持类型安全的事件发布与订阅。
/// 基于 Signal&lt;T&gt; 构建，替代原有的独立 EventBus 实现。
/// </summary>
public sealed class EventBus
{
    #region 字段

    private readonly Dictionary<Type, object> _signals = new();
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
            if (!_signals.TryGetValue(eventType, out var signal))
            {
                signal = new Signal<TEvent>();
                _signals[eventType] = signal;
            }

            ((Signal<TEvent>)signal).Connect(handler);
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
            if (_signals.TryGetValue(typeof(TEvent), out var signal))
            {
                ((Signal<TEvent>)signal).Disconnect(handler);
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
        Signal<TEvent>? signal;

        lock (_lock)
        {
            if (!_signals.TryGetValue(typeof(TEvent), out var s))
            {
                return;
            }

            signal = (Signal<TEvent>)s;
        }

        signal.Emit(evt);
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
            _signals.Clear();
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
            _signals.Remove(typeof(TEvent));
        }
    }

    #endregion
}

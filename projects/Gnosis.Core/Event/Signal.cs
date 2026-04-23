using System;
using System.Collections.Generic;

namespace Gnosis.Core.Event;

/// <summary>
/// 类型安全的信号机制，支持无参数和带参数两种模式。
/// 替代原有的 Signal、Signal&lt;T&gt;、MulticastDelegate&lt;T&gt; 和 EventBus 四套机制。
/// </summary>
public sealed class Signal
{
    private readonly List<Action> _handlers = [];
    private readonly List<Action> _pendingRemovals = [];
    private bool _isEmitting;

    /// <summary>
    /// 连接一个无参数处理器
    /// </summary>
    public ConnectionHandle Connect(Action handler)
    {
        _handlers.Add(handler);
        return new ConnectionHandle(() => Disconnect(handler));
    }

    /// <summary>
    /// 断开指定处理器
    /// </summary>
    public void Disconnect(Action handler)
    {
        if (_isEmitting)
        {
            _pendingRemovals.Add(handler);
        }
        else
        {
            _handlers.Remove(handler);
        }
    }

    /// <summary>
    /// 触发信号
    /// </summary>
    public void Emit()
    {
        _isEmitting = true;

        try
        {
            foreach (var handler in _handlers)
            {
                if (!_pendingRemovals.Contains(handler))
                {
                    handler();
                }
            }
        }
        finally
        {
            _isEmitting = false;

            foreach (var removal in _pendingRemovals)
            {
                _handlers.Remove(removal);
            }

            _pendingRemovals.Clear();
        }
    }

    /// <summary>
    /// 清空所有处理器
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
        _pendingRemovals.Clear();
    }

    /// <summary>
    /// 获取当前连接数
    /// </summary>
    public int ConnectionCount => _handlers.Count;
}

/// <summary>
/// 带参数的类型安全信号机制
/// </summary>
public sealed class Signal<T>
{
    private readonly List<Action<T>> _handlers = [];
    private readonly List<Action<T>> _pendingRemovals = [];
    private bool _isEmitting;

    /// <summary>
    /// 连接一个带参数的处理器
    /// </summary>
    public ConnectionHandle Connect(Action<T> handler)
    {
        _handlers.Add(handler);
        return new ConnectionHandle(() => Disconnect(handler));
    }

    /// <summary>
    /// 断开指定处理器
    /// </summary>
    public void Disconnect(Action<T> handler)
    {
        if (_isEmitting)
        {
            _pendingRemovals.Add(handler);
        }
        else
        {
            _handlers.Remove(handler);
        }
    }

    /// <summary>
    /// 触发信号，传递参数
    /// </summary>
    public void Emit(T value)
    {
        _isEmitting = true;

        try
        {
            foreach (var handler in _handlers)
            {
                if (!_pendingRemovals.Contains(handler))
                {
                    handler(value);
                }
            }
        }
        finally
        {
            _isEmitting = false;

            foreach (var removal in _pendingRemovals)
            {
                _handlers.Remove(removal);
            }

            _pendingRemovals.Clear();
        }
    }

    /// <summary>
    /// 清空所有处理器
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
        _pendingRemovals.Clear();
    }

    /// <summary>
    /// 获取当前连接数
    /// </summary>
    public int ConnectionCount => _handlers.Count;
}

using System.Collections.Generic;

namespace Gnosis.Core.Event;

/// <summary>
/// 轻量级带参数信号，实现观察者模式
/// </summary>
/// <typeparam name="T">信号参数类型</typeparam>
public class Signal<T>
{
    private readonly List<Action<T>> _handlers = new();
    private readonly List<int> _indexToId = new();
    private readonly Dictionary<int, int> _idToIndex = new();
    private int _nextId;

    /// <summary>
    /// 连接一个带参数的处理器到信号
    /// </summary>
    /// <param name="handler">要连接的处理器</param>
    /// <returns>连接句柄，用于后续断开连接</returns>
    public ConnectionHandle Connect(Action<T> handler)
    {
        var id = _nextId++;
        var index = _handlers.Count;
        _handlers.Add(handler);
        _indexToId.Add(id);
        _idToIndex[id] = index;
        return new ConnectionHandle { Id = id, Index = index };
    }

    /// <summary>
    /// 断开指定连接句柄对应的处理器，使用交换移除策略实现 O(1) 复杂度
    /// </summary>
    /// <param name="handle">要断开的连接句柄</param>
    public void Disconnect(ConnectionHandle handle)
    {
        if (!_idToIndex.TryGetValue(handle.Id, out var index))
        {
            return;
        }

        var lastIndex = _handlers.Count - 1;
        if (index != lastIndex)
        {
            _handlers[index] = _handlers[lastIndex];

            var swappedId = _indexToId[lastIndex];
            _indexToId[index] = swappedId;
            _idToIndex[swappedId] = index;
        }

        _handlers.RemoveAt(lastIndex);
        _indexToId.RemoveAt(lastIndex);
        _idToIndex.Remove(handle.Id);
    }

    /// <summary>
    /// 发射信号，按连接顺序调用所有处理器
    /// </summary>
    /// <param name="value">传递给处理器的值</param>
    public void Emit(T value)
    {
        for (var i = 0; i < _handlers.Count; i++)
        {
            _handlers[i](value);
        }
    }
}

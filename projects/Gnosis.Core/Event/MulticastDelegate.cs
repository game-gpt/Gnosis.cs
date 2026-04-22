using System.Collections.Generic;

namespace Gnosis.Core.Event;

/// <summary>
/// 轻量级多播委托，支持添加、移除和调用多个委托
/// </summary>
/// <typeparam name="T">委托类型</typeparam>
public class MulticastDelegate<T> where T : Delegate
{
    private readonly List<T> _handlers = new();

    /// <summary>
    /// 是否存在已注册的处理器
    /// </summary>
    public bool HasHandlers => _handlers.Count > 0;

    /// <summary>
    /// 已注册的处理器数量
    /// </summary>
    public int HandlerCount => _handlers.Count;

    /// <summary>
    /// 添加一个处理器
    /// </summary>
    /// <param name="handler">要添加的委托</param>
    public void Add(T handler)
    {
        _handlers.Add(handler);
    }

    /// <summary>
    /// 移除一个处理器
    /// </summary>
    /// <param name="handler">要移除的委托</param>
    public void Remove(T handler)
    {
        _handlers.Remove(handler);
    }

    /// <summary>
    /// 调用所有已注册的处理器
    /// </summary>
    /// <param name="args">传递给处理器的参数</param>
    public void Invoke(params object[] args)
    {
        for (var i = 0; i < _handlers.Count; i++)
        {
            _handlers[i]?.DynamicInvoke(args);
        }
    }
}

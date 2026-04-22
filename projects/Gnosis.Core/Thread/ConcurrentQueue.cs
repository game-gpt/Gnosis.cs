using System.Runtime.CompilerServices;

namespace Gnosis.Core.Thread;

/// <summary>
/// 无锁队列，基于 CAS 操作实现的多生产者多消费者队列
/// </summary>
public sealed class ConcurrentQueue<T>
{
    #region 嵌套类型

    private sealed class Node
    {
        public T Value;
        public Node? Next;

        public Node()
        {
            Value = default!;
            Next = null;
        }

        public Node(T value)
        {
            Value = value;
            Next = null;
        }
    }

    #endregion

    #region 字段

    private Node _head;
    private Node _tail;

    #endregion

    #region 属性

    /// <summary>
    /// 队列是否为空
    /// </summary>
    public bool IsEmpty => _head.Next is null;

    #endregion

    #region 构造函数

    public ConcurrentQueue()
    {
        _head = new Node();
        _tail = _head;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 入队
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(T item)
    {
        var node = new Node(item);

        Node tail;
        Node next;

        while (true)
        {
            tail = _tail;
            next = tail.Next!;

            if (tail != _tail)
            {
                continue;
            }

            if (next is null)
            {
                if (System.Threading.Interlocked.CompareExchange(ref tail.Next, node, null) == null)
                {
                    System.Threading.Interlocked.CompareExchange(ref _tail, node, tail);
                    break;
                }
            }
            else
            {
                System.Threading.Interlocked.CompareExchange(ref _tail, next, tail);
            }
        }
    }

    /// <summary>
    /// 尝试出队
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDequeue(out T item)
    {
        while (true)
        {
            var head = _head;
            var tail = _tail;
            var next = head.Next;

            if (head != _head)
            {
                continue;
            }

            if (head == tail)
            {
                if (next is null)
                {
                    item = default!;
                    return false;
                }

                System.Threading.Interlocked.CompareExchange(ref _tail, next, tail);
            }
            else
            {
                item = next!.Value;
                if (System.Threading.Interlocked.CompareExchange(ref _head, next, head) == head)
                {
                    break;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 尝试查看队首元素
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeek(out T item)
    {
        var next = _head.Next;
        if (next is null)
        {
            item = default!;
            return false;
        }

        item = next.Value;
        return true;
    }

    #endregion
}

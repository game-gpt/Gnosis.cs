using System.Runtime.CompilerServices;

namespace Gnosis.Core.Collection;

/// <summary>
/// 通用对象池，支持预分配和自动扩展
/// </summary>
public sealed class ObjectPool<T> where T : class, IResettable
{
    #region 字段

    private readonly T[] _pool;
    private readonly Func<T> _factory;
    private int _availableCount;

    #endregion

    #region 属性

    /// <summary>
    /// 池中可用对象数量
    /// </summary>
    public int AvailableCount => _availableCount;

    /// <summary>
    /// 池的总容量
    /// </summary>
    public int Capacity => _pool.Length;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建对象池
    /// </summary>
    /// <param name="factory">对象工厂方法</param>
    /// <param name="capacity">池容量</param>
    /// <param name="preallocate">是否预分配对象</param>
    public ObjectPool(Func<T> factory, int capacity, bool preallocate = true)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "容量必须大于 0");
        }

        _factory = factory;
        _pool = new T[capacity];
        _availableCount = 0;

        if (preallocate)
        {
            for (var i = 0; i < capacity; i++)
            {
                _pool[_availableCount++] = factory();
            }
        }
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 从池中获取一个对象，池为空时创建新对象
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get()
    {
        if (_availableCount > 0)
        {
            var obj = _pool[--_availableCount];
            _pool[_availableCount] = null!;
            return obj;
        }

        return _factory();
    }

    /// <summary>
    /// 将对象归还到池中，自动调用 Reset
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(T obj)
    {
        if (obj is null)
        {
            return;
        }

        obj.Reset();

        if (_availableCount < _pool.Length)
        {
            _pool[_availableCount++] = obj;
        }
    }

    /// <summary>
    /// 清空对象池
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Array.Clear(_pool, 0, _availableCount);
        _availableCount = 0;
    }

    #endregion
}

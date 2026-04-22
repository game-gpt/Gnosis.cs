using System.Runtime.CompilerServices;

namespace Gnosis.Core.Thread;

/// <summary>
/// 原子整数，提供线程安全的整数操作
/// </summary>
public sealed class AtomicInt
{
    #region 字段

    private int _value;

    #endregion

    #region 属性

    /// <summary>
    /// 获取或设置当前值（非原子操作，仅用于初始化）
    /// </summary>
    public int Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => System.Threading.Interlocked.CompareExchange(ref _value, 0, 0);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => System.Threading.Interlocked.Exchange(ref _value, value);
    }

    #endregion

    #region 构造函数

    public AtomicInt(int value = 0)
    {
        _value = value;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 原子递增并返回递增后的值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Increment()
    {
        return System.Threading.Interlocked.Increment(ref _value);
    }

    /// <summary>
    /// 原子递减并返回递减后的值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Decrement()
    {
        return System.Threading.Interlocked.Decrement(ref _value);
    }

    /// <summary>
    /// 原子添加指定值并返回添加后的值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Add(int delta)
    {
        return System.Threading.Interlocked.Add(ref _value, delta);
    }

    /// <summary>
    /// 原子比较并交换，如果当前值等于 expected 则设置为 newValue
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CompareExchange(int expected, int newValue)
    {
        return System.Threading.Interlocked.CompareExchange(ref _value, newValue, expected) == expected;
    }

    #endregion
}

/// <summary>
/// 原子长整数，提供线程安全的长整数操作
/// </summary>
public sealed class AtomicLong
{
    #region 字段

    private long _value;

    #endregion

    #region 属性

    public long Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => System.Threading.Interlocked.Read(ref _value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => System.Threading.Interlocked.Exchange(ref _value, value);
    }

    #endregion

    #region 构造函数

    public AtomicLong(long value = 0)
    {
        _value = value;
    }

    #endregion

    #region 公开方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Increment()
    {
        return System.Threading.Interlocked.Increment(ref _value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Decrement()
    {
        return System.Threading.Interlocked.Decrement(ref _value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Add(long delta)
    {
        return System.Threading.Interlocked.Add(ref _value, delta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CompareExchange(long expected, long newValue)
    {
        return System.Threading.Interlocked.CompareExchange(ref _value, newValue, expected) == expected;
    }

    #endregion
}

/// <summary>
/// 原子布尔值，提供线程安全的布尔操作
/// </summary>
public sealed class AtomicBool
{
    #region 字段

    private int _value;

    #endregion

    #region 属性

    public bool Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => System.Threading.Interlocked.CompareExchange(ref _value, 0, 0) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => System.Threading.Interlocked.Exchange(ref _value, value ? 1 : 0);
    }

    #endregion

    #region 构造函数

    public AtomicBool(bool value = false)
    {
        _value = value ? 1 : 0;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 原子设置为 true 并返回旧值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SetTrue()
    {
        return System.Threading.Interlocked.Exchange(ref _value, 1) != 0;
    }

    /// <summary>
    /// 原子设置为 false 并返回旧值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SetFalse()
    {
        return System.Threading.Interlocked.Exchange(ref _value, 0) != 0;
    }

    /// <summary>
    /// 原子比较并交换
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CompareExchange(bool expected, bool newValue)
    {
        var expectedInt = expected ? 1 : 0;
        var newInt = newValue ? 1 : 0;
        return System.Threading.Interlocked.CompareExchange(ref _value, newInt, expectedInt) == expectedInt;
    }

    #endregion
}

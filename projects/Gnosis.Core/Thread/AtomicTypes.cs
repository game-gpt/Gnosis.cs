using System.Runtime.CompilerServices;

namespace Gnosis.Core.Thread;

public sealed class AtomicInt
{
    #region 字段

    private int _value;

    #endregion

    #region 属性

    public int Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => global::System.Threading.Interlocked.CompareExchange(ref _value, 0, 0);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => global::System.Threading.Interlocked.Exchange(ref _value, value);
    }

    #endregion

    #region 构造函数

    public AtomicInt(int value = 0)
    {
        _value = value;
    }

    #endregion

    #region 公开方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Increment()
    {
        return global::System.Threading.Interlocked.Increment(ref _value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Decrement()
    {
        return global::System.Threading.Interlocked.Decrement(ref _value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Add(int delta)
    {
        return global::System.Threading.Interlocked.Add(ref _value, delta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CompareExchange(int expected, int newValue)
    {
        return global::System.Threading.Interlocked.CompareExchange(ref _value, newValue, expected) == expected;
    }

    #endregion
}

public sealed class AtomicLong
{
    #region 字段

    private long _value;

    #endregion

    #region 属性

    public long Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => global::System.Threading.Interlocked.Read(ref _value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => global::System.Threading.Interlocked.Exchange(ref _value, value);
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
        return global::System.Threading.Interlocked.Increment(ref _value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Decrement()
    {
        return global::System.Threading.Interlocked.Decrement(ref _value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Add(long delta)
    {
        return global::System.Threading.Interlocked.Add(ref _value, delta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CompareExchange(long expected, long newValue)
    {
        return global::System.Threading.Interlocked.CompareExchange(ref _value, newValue, expected) == expected;
    }

    #endregion
}

public sealed class AtomicBool
{
    #region 字段

    private int _value;

    #endregion

    #region 属性

    public bool Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => global::System.Threading.Interlocked.CompareExchange(ref _value, 0, 0) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => global::System.Threading.Interlocked.Exchange(ref _value, value ? 1 : 0);
    }

    #endregion

    #region 构造函数

    public AtomicBool(bool value = false)
    {
        _value = value ? 1 : 0;
    }

    #endregion

    #region 公开方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SetTrue()
    {
        return global::System.Threading.Interlocked.Exchange(ref _value, 1) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SetFalse()
    {
        return global::System.Threading.Interlocked.Exchange(ref _value, 0) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CompareExchange(bool expected, bool newValue)
    {
        var expectedInt = expected ? 1 : 0;
        var newInt = newValue ? 1 : 0;
        return global::System.Threading.Interlocked.CompareExchange(ref _value, newInt, expectedInt) == expectedInt;
    }

    #endregion
}

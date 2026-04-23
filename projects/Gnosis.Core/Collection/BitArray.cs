using System.Runtime.CompilerServices;

namespace Gnosis.Core.Collection;

/// <summary>
/// 位数组，支持按位操作和高效集合运算
/// </summary>
public sealed class BitArray
{
    #region 常量

    private const int BitsPerWord = 64;
    private const int ShiftBits = 6;
    private const int MaskBits = 63;

    #endregion

    #region 字段

    private ulong[] _words;

    #endregion

    #region 属性

    /// <summary>
    /// 位数组的位长度
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// 位数组中设置为 1 的位数
    /// </summary>
    public int PopCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < _words.Length; i++)
            {
                count += global::System.Numerics.BitOperations.PopCount(_words[i]);
            }

            return count;
        }
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建指定位数的位数组
    /// </summary>
    public BitArray(int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "长度不能为负数");
        }

        Length = length;
        _words = new ulong[(length + BitsPerWord - 1) >> ShiftBits];
    }

    /// <summary>
    /// 从已有位数组创建副本
    /// </summary>
    public BitArray(BitArray other)
    {
        Length = other.Length;
        _words = new ulong[other._words.Length];
        Array.Copy(other._words, _words, _words.Length);
    }

    #endregion

    #region 索引器

    /// <summary>
    /// 获取或设置指定位置的位
    /// </summary>
    public bool this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Get(index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Set(index, value);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 获取指定位置的位
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(int index)
    {
        if ((uint)index >= (uint)Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "索引超出范围");
        }

        return (_words[index >> ShiftBits] & (1UL << (index & MaskBits))) != 0;
    }

    /// <summary>
    /// 设置指定位置的位
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, bool value)
    {
        if ((uint)index >= (uint)Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "索引超出范围");
        }

        if (value)
        {
            _words[index >> ShiftBits] |= 1UL << (index & MaskBits);
        }
        else
        {
            _words[index >> ShiftBits] &= ~(1UL << (index & MaskBits));
        }
    }

    /// <summary>
    /// 将所有位设置为指定值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAll(bool value)
    {
        var fill = value ? ulong.MaxValue : 0UL;
        Array.Fill(_words, fill);

        ClearUnusedBits();
    }

    /// <summary>
    /// 按位与运算
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void And(BitArray other)
    {
        ValidateSameLength(other);
        for (var i = 0; i < _words.Length; i++)
        {
            _words[i] &= other._words[i];
        }
    }

    /// <summary>
    /// 按位或运算
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Or(BitArray other)
    {
        ValidateSameLength(other);
        for (var i = 0; i < _words.Length; i++)
        {
            _words[i] |= other._words[i];
        }
    }

    /// <summary>
    /// 按位异或运算
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Xor(BitArray other)
    {
        ValidateSameLength(other);
        for (var i = 0; i < _words.Length; i++)
        {
            _words[i] ^= other._words[i];
        }
    }

    /// <summary>
    /// 按位取反
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Not()
    {
        for (var i = 0; i < _words.Length; i++)
        {
            _words[i] = ~_words[i];
        }

        ClearUnusedBits();
    }

    /// <summary>
    /// 清除所有位
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Array.Clear(_words);
    }

    #endregion

    #region 私有方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateSameLength(BitArray other)
    {
        if (other.Length != Length)
        {
            throw new ArgumentException("位数组长度不一致");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearUnusedBits()
    {
        var remaining = Length & MaskBits;
        if (remaining == 0)
        {
            return;
        }

        _words[^1] &= (1UL << remaining) - 1;
    }

    #endregion
}

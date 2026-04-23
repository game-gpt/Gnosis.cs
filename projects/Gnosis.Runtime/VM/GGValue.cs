using System.Runtime.CompilerServices;

namespace Gnosis.Runtime.VM;

public enum GGValueType : byte
{
    Null,
    Int,
    Float,
    Bool,
    String,
    Object,
    Array,
    Struct,
    Closure,
    NativeObject,
    Entity
}

/// <summary>
/// NaN-Boxing 混合值类型，使用 IEEE 754 双精度浮点数的 NaN 编码空间存储标签，
/// 同时保留 _reference 字段供 GC 追踪引用类型。
///
/// 位布局（64 位 _bits）：
///   正常 double：直接存储 IEEE 754 位模式（非 NaN 空间）
///   NaN double：规范化为 0x7FF8_0000_0000_0000
///   标签值：    0x7FF9~0x7FFF（利用 quiet NaN 的冗余编码空间）
///
/// 标签编码（高 16 位）：
///   0x7FF8 = Float（规范化 NaN）
///   0x7FF9 = Int（低 48 位存储有符号整数，支持 48 位范围）
///   0x7FFA = Bool（payload = 0 或 1）
///   0x7FFB = Null
///   0x7FFC = Entity（低 48 位存储实体 ID）
///   0x7FFD = Ref（GC 引用类型，对象存储在 _reference）
///   0x7FFE = NativeRef（非 GC 原生引用，对象存储在 _reference）
///   0x7FFF = 保留
/// </summary>
public readonly struct GGValue : IEquatable<GGValue>
{
    #region 常量

    private const ulong TagMask = 0xFFFF_0000_0000_0000UL;
    private const ulong PayloadMask = 0x0000_FFFF_FFFF_FFFFUL;
    private const ulong NoSignTagMask = 0x7FFF_0000_0000_0000UL;

    private const ulong DoubleNaNTag = 0x7FF8_0000_0000_0000UL;
    private const ulong IntTag = 0x7FF9_0000_0000_0000UL;
    private const ulong BoolTag = 0x7FFA_0000_0000_0000UL;
    private const ulong NullTag = 0x7FFB_0000_0000_0000UL;
    private const ulong EntityTag = 0x7FFC_0000_0000_0000UL;
    private const ulong RefTag = 0x7FFD_0000_0000_0000UL;
    private const ulong NativeRefTag = 0x7FFE_0000_0000_0000UL;

    private const ulong BoolTruePayload = 1UL;

    #endregion

    #region 字段

    private readonly ulong _bits;
    private readonly object? _reference;

    #endregion

    #region 构造函数

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GGValue(ulong bits, object? reference)
    {
        _bits = bits;
        _reference = reference;
    }

    #endregion

    #region 工厂方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GGValue FromInt(long value)
    {
        return new GGValue(IntTag | ((ulong)value & PayloadMask), null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GGValue FromFloat(double value)
    {
        if (double.IsNaN(value))
        {
            return new GGValue(DoubleNaNTag, null);
        }

        return new GGValue((ulong)BitConverter.DoubleToInt64Bits(value), null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GGValue FromBool(bool value)
    {
        return new GGValue(BoolTag | (value ? BoolTruePayload : 0UL), null);
    }

    public static GGValue Null => new(NullTag, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GGValue FromString(GGString value)
    {
        return new GGValue(RefTag, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GGValue FromObject(GGObject value)
    {
        return new GGValue(RefTag, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GGValue FromArray(GGArray value)
    {
        return new GGValue(RefTag, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GGValue FromStruct(GGStruct value)
    {
        return new GGValue(RefTag, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GGValue FromClosure(GGClosure value)
    {
        return new GGValue(RefTag, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GGValue FromNativeObject(object value)
    {
        return new GGValue(NativeRefTag, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GGValue FromEntity(int entityId)
    {
        return new GGValue(EntityTag | (ulong)entityId, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GGValue FromEntity(long entityId)
    {
        return new GGValue(EntityTag | ((ulong)entityId & PayloadMask), null);
    }

    #endregion

    #region 类型判断

    public GGValueType Type
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var noSign = _bits & NoSignTagMask;

            if (noSign < DoubleNaNTag)
            {
                return GGValueType.Float;
            }

            if (noSign == DoubleNaNTag)
            {
                return GGValueType.Float;
            }

            var tag = _bits & TagMask;
            return tag switch
            {
                IntTag => GGValueType.Int,
                BoolTag => GGValueType.Bool,
                NullTag => GGValueType.Null,
                EntityTag => GGValueType.Entity,
                RefTag => ClassifyReference(),
                NativeRefTag => GGValueType.NativeObject,
                _ => GGValueType.Null
            };
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GGValueType ClassifyReference()
    {
        return _reference switch
        {
            null => GGValueType.Null,
            GGString => GGValueType.String,
            GGObject => GGValueType.Object,
            GGArray => GGValueType.Array,
            GGStruct => GGValueType.Struct,
            GGClosure => GGValueType.Closure,
            _ => GGValueType.NativeObject
        };
    }

    public bool IsNull => (_bits & TagMask) == NullTag;

    public bool IsFloat => (_bits & NoSignTagMask) <= DoubleNaNTag;

    public bool IsInt => (_bits & TagMask) == IntTag;

    public bool IsBool => (_bits & TagMask) == BoolTag;

    public bool IsEntity => (_bits & TagMask) == EntityTag;

    public bool IsReference => (_bits & TagMask) == RefTag || (_bits & TagMask) == NativeRefTag;

    #endregion

    #region 值访问

    public long IntValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (long)((_bits & PayloadMask) << 16) >> 16;
    }

    public double FloatValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BitConverter.Int64BitsToDouble((long)_bits);
    }

    public bool BoolValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_bits & PayloadMask) != 0;
    }

    public int EntityId
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(_bits & PayloadMask);
    }

    public GGString? StringValue => _reference as GGString;

    public GGObject? ObjectValue => _reference as GGObject;

    public GGArray? ArrayValue => _reference as GGArray;

    public GGStruct? StructValue => _reference as GGStruct;

    public GGClosure? ClosureValue => _reference as GGClosure;

    public object? NativeObjectValue => (_bits & TagMask) == NativeRefTag ? _reference : null;

    public object? Reference => _reference;

    #endregion

    #region 真值判断

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsTruthy()
    {
        var tag = _bits & TagMask;

        if (tag == NullTag)
        {
            return false;
        }

        if (tag == BoolTag)
        {
            return (_bits & PayloadMask) != 0;
        }

        if (tag == IntTag)
        {
            return IntValue != 0;
        }

        if (tag <= DoubleNaNTag)
        {
            return FloatValue != 0.0;
        }

        return true;
    }

    #endregion

    #region 相等性

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(GGValue other)
    {
        if (_bits != other._bits)
        {
            return false;
        }

        if ((_bits & TagMask) is RefTag or NativeRefTag)
        {
            return ReferenceEquals(_reference, other._reference);
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is GGValue other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_bits, _reference);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(GGValue left, GGValue right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(GGValue left, GGValue right)
    {
        return !left.Equals(right);
    }

    #endregion

    #region 转换辅助

    public override string? ToString()
    {
        var tag = _bits & TagMask;

        return tag switch
        {
            NullTag => "null",
            IntTag => IntValue.ToString(),
            _ when tag <= DoubleNaNTag => FloatValue.ToString("G"),
            BoolTag => BoolValue ? "true" : "false",
            EntityTag => $"entity:{EntityId}",
            RefTag => _reference?.ToString() ?? "null",
            NativeRefTag => _reference?.ToString() ?? "null",
            _ => "unknown"
        };
    }

    #endregion
}

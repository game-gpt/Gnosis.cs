using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.Interop;

/// <summary>
/// 值类型封送器，处理 C# 值与 GGValue 之间的双向转换
/// </summary>
public static class Marshaller
{
    public static GGValue ToGGValue(object? value)
    {
        return ReferenceMarshaller.MarshalToGG(value);
    }

    public static object? FromGGValue(GGValue value)
    {
        return ReferenceMarshaller.MarshalFromGG(value);
    }

    public static GGValue[] ToGGValues(object?[] values)
    {
        var result = new GGValue[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = ToGGValue(values[i]);
        }
        return result;
    }

    public static object?[] FromGGValues(GGValue[] values)
    {
        var result = new object?[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = FromGGValue(values[i]);
        }
        return result;
    }

    public static long ToInt64(GGValue value)
    {
        if (value.IsInt) return value.IntValue;
        if (value.IsFloat) return (long)value.FloatValue;
        if (value.IsBool) return value.BoolValue ? 1 : 0;
        return 0;
    }

    public static double ToFloat64(GGValue value)
    {
        if (value.IsFloat) return value.FloatValue;
        if (value.IsInt) return value.IntValue;
        return 0.0;
    }

    public static bool ToBool(GGValue value)
    {
        return value.IsTruthy();
    }
}

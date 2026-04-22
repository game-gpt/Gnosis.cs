namespace Gnosis.Runtime.Interop;

public static class Marshaller
{
    public static object? ToGGValue(object? value)
    {
        return value switch
        {
            null => null,
            int i => (long)i,
            long l => l,
            float f => (double)f,
            double d => d,
            bool b => b ? 1L : 0L,
            string s => s,
            _ => value
        };
    }

    public static object? FromGGValue(object? value)
    {
        return value switch
        {
            long l => l,
            double d => d,
            string s => s,
            _ => value
        };
    }

    public static object?[] ToGGValues(object?[] values)
    {
        var result = new object?[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = ToGGValue(values[i]);
        }
        return result;
    }

    public static object?[] FromGGValues(object?[] values)
    {
        var result = new object?[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = FromGGValue(values[i]);
        }
        return result;
    }

    public static long ToInt64(object? value)
    {
        return value switch
        {
            long l => l,
            int i => i,
            short s => s,
            byte b => b,
            double d => (long)d,
            float f => (long)f,
            bool b2 => b2 ? 1 : 0,
            _ => 0
        };
    }

    public static double ToFloat64(object? value)
    {
        return value switch
        {
            double d => d,
            float f => f,
            long l => l,
            int i => i,
            _ => 0.0
        };
    }

    public static bool ToBool(object? value)
    {
        return value switch
        {
            bool b => b,
            long l => l != 0,
            int i => i != 0,
            double d => d != 0.0,
            null => false,
            _ => true
        };
    }

    public static string? ToString(object? value)
    {
        return value?.ToString();
    }
}

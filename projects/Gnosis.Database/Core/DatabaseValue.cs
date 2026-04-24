using LightValueInner = LightDB.Core.LightValue;

namespace Gnosis.Database.Core;

public readonly record struct DatabaseValue
{
    private readonly LightValueInner _inner;

    public DatabaseValue(ReadOnlyMemory<byte> bytes)
    {
        _inner = new LightValueInner(bytes.ToArray());
    }

    public DatabaseValue(LightValueInner value)
    {
        _inner = value;
    }

    public ReadOnlyMemory<byte> Bytes => _inner.Bytes;

    public int Length => _inner.Length;

    public bool IsEmpty => _inner.IsEmpty;

    public static DatabaseValue Empty => new(LightValueInner.Empty);

    public static DatabaseValue FromString(string value) => new(LightValueInner.FromString(value));

    public static DatabaseValue FromInt32(int value) => new(LightValueInner.FromInt32(value));

    public static DatabaseValue FromInt64(long value) => new(LightValueInner.FromInt64(value));

    public static DatabaseValue FromDouble(double value) => new(LightValueInner.FromDouble(value));

    public static DatabaseValue FromObject<T>(T value) => new(LightValueInner.FromObject(value));

    public T? ToObject<T>() => _inner.ToObject<T>();

    public int CompareTo(DatabaseValue other)
    {
        var thisSpan = Bytes.Span;
        var otherSpan = other.Bytes.Span;
        var minLength = Math.Min(thisSpan.Length, otherSpan.Length);

        for (var i = 0; i < minLength; i++)
        {
            var cmp = thisSpan[i].CompareTo(otherSpan[i]);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        return thisSpan.Length.CompareTo(otherSpan.Length);
    }

    public static implicit operator LightValueInner(DatabaseValue value) => value._inner;

    public static implicit operator DatabaseValue(LightValueInner value) => new(value);

    public override string ToString() => _inner.ToString();
}

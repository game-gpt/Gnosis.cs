using SolidValueInner = SolidDB.Core.SolidValue;

namespace Gnosis.Database.Core;

public readonly record struct DatabaseValue
{
    private readonly SolidValueInner _inner;

    public DatabaseValue(ReadOnlyMemory<byte> bytes)
    {
        _inner = new SolidValueInner(bytes.ToArray());
    }

    public DatabaseValue(SolidValueInner value)
    {
        _inner = value;
    }

    public ReadOnlyMemory<byte> Bytes => _inner.Bytes;

    public int Length => _inner.Length;

    public bool IsEmpty => _inner.IsEmpty;

    public static DatabaseValue Empty => new(SolidValueInner.Empty);

    public static DatabaseValue FromString(string value) => new(SolidValueInner.FromString(value));

    public static DatabaseValue FromInt32(int value) => new(SolidValueInner.FromInt32(value));

    public static DatabaseValue FromInt64(long value) => new(SolidValueInner.FromInt64(value));

    public static DatabaseValue FromDouble(double value) => new(SolidValueInner.FromDouble(value));

    public static DatabaseValue FromObject<T>(T value) => new(SolidValueInner.FromObject(value));

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

    public static implicit operator SolidValueInner(DatabaseValue value) => value._inner;

    public static implicit operator DatabaseValue(SolidValueInner value) => new(value);

    public override string ToString() => _inner.ToString();
}

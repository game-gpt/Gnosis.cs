namespace Gnosis.Database.Core;

public readonly record struct DatabaseValue(ReadOnlyMemory<byte> Bytes)
{
    public int Length => Bytes.Length;

    public bool IsEmpty => Bytes.IsEmpty;

    public static DatabaseValue Empty => new(ReadOnlyMemory<byte>.Empty);

    public static DatabaseValue FromString(string value) =>
        new(System.Text.Encoding.UTF8.GetBytes(value));

    public static DatabaseValue FromInt32(int value) =>
        new(BitConverter.GetBytes(value));

    public static DatabaseValue FromInt64(long value) =>
        new(BitConverter.GetBytes(value));

    public static DatabaseValue FromDouble(double value) =>
        new(BitConverter.GetBytes(value));

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
}

namespace Gnosis.Database.Core;

public readonly record struct DatabaseKey(ReadOnlyMemory<byte> Bytes)
{
    public int Length => Bytes.Length;

    public bool IsEmpty => Bytes.IsEmpty;

    public static DatabaseKey Empty => new(ReadOnlyMemory<byte>.Empty);

    public static DatabaseKey FromString(string value) =>
        new(System.Text.Encoding.UTF8.GetBytes(value));

    public static DatabaseKey FromUInt64(ulong value) =>
        new(BitConverter.GetBytes(value));

    public static DatabaseKey FromGuid(Guid value) =>
        new(value.ToByteArray());

    public bool StartsWith(DatabaseKey prefix)
    {
        if (prefix.Length > Length)
        {
            return false;
        }

        var span = Bytes.Span;
        var prefixSpan = prefix.Bytes.Span;
        for (var i = 0; i < prefix.Length; i++)
        {
            if (span[i] != prefixSpan[i])
            {
                return false;
            }
        }

        return true;
    }

    public int CompareTo(DatabaseKey other)
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

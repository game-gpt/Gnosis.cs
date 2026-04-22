using SolidKeyInner = SolidDB.Core.SolidKey;

namespace Gnosis.Database.Core;

public readonly record struct DatabaseKey
{
    private readonly SolidKeyInner _inner;

    public DatabaseKey(ReadOnlyMemory<byte> bytes)
    {
        _inner = new SolidKeyInner(bytes.ToArray());
    }

    public DatabaseKey(SolidKeyInner key)
    {
        _inner = key;
    }

    public ReadOnlyMemory<byte> Bytes => _inner.Bytes;

    public int Length => _inner.Length;

    public bool IsEmpty => _inner.IsEmpty;

    public static DatabaseKey Empty => new(SolidKeyInner.Empty);

    public static DatabaseKey FromString(string value) => new(SolidKeyInner.FromString(value));

    public static DatabaseKey FromUInt64(ulong value) => new(SolidKeyInner.FromUInt64(value));

    public static DatabaseKey FromGuid(Guid value) => new(SolidKeyInner.FromGuid(value));

    public bool StartsWith(DatabaseKey prefix)
    {
        return _inner.StartsWith(prefix._inner);
    }

    public int CompareTo(DatabaseKey other)
    {
        return _inner.CompareTo(other._inner);
    }

    public static implicit operator SolidKeyInner(DatabaseKey key) => key._inner;

    public static implicit operator DatabaseKey(SolidKeyInner key) => new(key);

    public override string ToString() => _inner.ToString();
}

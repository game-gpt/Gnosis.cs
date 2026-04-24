using LightKeyInner = LightDB.Core.LightKey;

namespace Gnosis.Database.Core;

public readonly record struct DatabaseKey
{
    private readonly LightKeyInner _inner;

    public DatabaseKey(ReadOnlyMemory<byte> bytes)
    {
        _inner = new LightKeyInner(bytes.ToArray());
    }

    public DatabaseKey(LightKeyInner key)
    {
        _inner = key;
    }

    public ReadOnlyMemory<byte> Bytes => _inner.Bytes;

    public int Length => _inner.Length;

    public bool IsEmpty => _inner.IsEmpty;

    public static DatabaseKey Empty => new(LightKeyInner.Empty);

    public static DatabaseKey FromString(string value) => new(LightKeyInner.FromString(value));

    public static DatabaseKey FromUInt64(ulong value) => new(LightKeyInner.FromUInt64(value));

    public static DatabaseKey FromGuid(Guid value) => new(LightKeyInner.FromGuid(value));

    public bool StartsWith(DatabaseKey prefix)
    {
        return _inner.StartsWith(prefix._inner);
    }

    public int CompareTo(DatabaseKey other)
    {
        return _inner.CompareTo(other._inner);
    }

    public static implicit operator LightKeyInner(DatabaseKey key) => key._inner;

    public static implicit operator DatabaseKey(LightKeyInner key) => new(key);

    public override string ToString() => _inner.ToString();
}

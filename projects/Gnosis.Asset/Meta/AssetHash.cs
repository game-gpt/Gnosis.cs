namespace Gnosis.Asset.Meta;

public readonly record struct AssetHash
{
    public byte[] Value { get; init; }

    public AssetHash(byte[] value)
    {
        Value = value ?? Array.Empty<byte>();
    }

    public static AssetHash Empty => new(Array.Empty<byte>());

    public bool IsEmpty => Value.Length == 0;

    public override string ToString()
    {
        return Convert.ToHexString(Value).ToLowerInvariant();
    }

    public bool Equals(AssetHash other)
    {
        if (Value.Length != other.Value.Length)
        {
            return false;
        }

        for (var i = 0; i < Value.Length; i++)
        {
            if (Value[i] != other.Value[i])
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var b in Value)
        {
            hash.Add(b);
        }

        return hash.ToHashCode();
    }

    public static AssetHash Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new ArgumentException("哈希字符串不能为空", nameof(s));
        }

        var bytes = Convert.FromHexString(s);
        return new AssetHash(bytes);
    }

    public static bool TryParse(string s, out AssetHash hash)
    {
        try
        {
            hash = Parse(s);
            return true;
        }
        catch
        {
            hash = Empty;
            return false;
        }
    }
}

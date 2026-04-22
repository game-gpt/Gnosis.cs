namespace Gnosis.Asset.Meta;

public readonly record struct AssetGuid
{
    public ulong Low { get; init; }
    public ulong High { get; init; }

    public AssetGuid(ulong low, ulong high)
    {
        Low = low;
        High = high;
    }

    public static AssetGuid NewGuid()
    {
        var bytes = new byte[16];
        Random.Shared.NextBytes(bytes);
        var low = BitConverter.ToUInt64(bytes, 0);
        var high = BitConverter.ToUInt64(bytes, 8);
        return new AssetGuid(low, high);
    }

    public static AssetGuid Empty => new(0, 0);

    public bool IsEmpty => Low == 0 && High == 0;

    public override string ToString()
    {
        return $"{High:X16}{Low:X16}";
    }

    public static AssetGuid Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new ArgumentException("GUID 字符串不能为空", nameof(s));
        }

        if (s.Length != 32)
        {
            throw new FormatException($"GUID 字符串长度必须为 32，实际为 {s.Length}");
        }

        var high = ulong.Parse(s[..16], System.Globalization.NumberStyles.HexNumber);
        var low = ulong.Parse(s[16..], System.Globalization.NumberStyles.HexNumber);
        return new AssetGuid(low, high);
    }

    public static bool TryParse(string s, out AssetGuid guid)
    {
        try
        {
            guid = Parse(s);
            return true;
        }
        catch
        {
            guid = Empty;
            return false;
        }
    }
}

using System.Buffers;
using Gnosis.Database.Core;

namespace Gnosis.Database.BTree;

public static class PrefixCompressor
{
    #region 公开方法

    public static int ComputeCommonPrefixLength(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var minLength = Math.Min(a.Length, b.Length);
        var i = 0;

        for (; i < minLength; i++)
        {
            if (a[i] != b[i])
            {
                break;
            }
        }

        return i;
    }

    public static int ComputeCommonPrefixLength(ReadOnlySpan<DatabaseKey> keys)
    {
        if (keys.Length == 0)
        {
            return 0;
        }

        if (keys.Length == 1)
        {
            return keys[0].Length;
        }

        var prefixLength = keys[0].Length;
        var firstSpan = keys[0].Bytes.Span;

        for (var i = 1; i < keys.Length && prefixLength > 0; i++)
        {
            var currentSpan = keys[i].Bytes.Span;
            var currentPrefix = 0;
            var maxCheck = Math.Min(prefixLength, currentSpan.Length);

            for (var j = 0; j < maxCheck; j++)
            {
                if (firstSpan[j] != currentSpan[j])
                {
                    break;
                }

                currentPrefix++;
            }

            prefixLength = currentPrefix;
        }

        return prefixLength;
    }

    public static CompressedKeys CompressKeys(ReadOnlySpan<DatabaseKey> keys)
    {
        if (keys.Length == 0)
        {
            return new CompressedKeys(0, []);
        }

        var prefixLength = ComputeCommonPrefixLength(keys);

        if (prefixLength == 0)
        {
            var uncompressed = new DatabaseKey[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                uncompressed[i] = keys[i];
            }

            return new CompressedKeys(0, uncompressed);
        }

        var firstKey = keys[0];
        var firstSpan = firstKey.Bytes.Span;
        var prefixBytes = new byte[prefixLength];
        firstSpan[..prefixLength].CopyTo(prefixBytes);

        var suffixes = new DatabaseKey[keys.Length];

        for (var i = 0; i < keys.Length; i++)
        {
            var keySpan = keys[i].Bytes.Span;
            if (keySpan.Length <= prefixLength)
            {
                suffixes[i] = DatabaseKey.Empty;
            }
            else
            {
                var suffix = new byte[keySpan.Length - prefixLength];
                keySpan[prefixLength..].CopyTo(suffix);
                suffixes[i] = new DatabaseKey(suffix);
            }
        }

        return new CompressedKeys(prefixLength, suffixes, prefixBytes);
    }

    public static DatabaseKey[] DecompressKeys(CompressedKeys compressed)
    {
        if (compressed.PrefixLength == 0)
        {
            var result = new DatabaseKey[compressed.Suffixes.Length];
            for (var i = 0; i < compressed.Suffixes.Length; i++)
            {
                result[i] = compressed.Suffixes[i];
            }

            return result;
        }

        var prefix = compressed.PrefixBytes;
        var keys = new DatabaseKey[compressed.Suffixes.Length];

        for (var i = 0; i < compressed.Suffixes.Length; i++)
        {
            var suffix = compressed.Suffixes[i];
            if (suffix.IsEmpty)
            {
                keys[i] = new DatabaseKey(prefix);
            }
            else
            {
                var combined = new byte[prefix.Length + suffix.Length];
                prefix.CopyTo(combined, 0);
                suffix.Bytes.Span.CopyTo(combined.AsSpan(prefix.Length));
                keys[i] = new DatabaseKey(combined);
            }
        }

        return keys;
    }

    public static DatabaseKey ReconstructKey(CompressedKeys compressed, int index)
    {
        if (compressed.PrefixLength == 0)
        {
            return compressed.Suffixes[index];
        }

        var suffix = compressed.Suffixes[index];
        if (suffix.IsEmpty)
        {
            return new DatabaseKey(compressed.PrefixBytes);
        }

        var combined = new byte[compressed.PrefixBytes.Length + suffix.Length];
        compressed.PrefixBytes.CopyTo(combined, 0);
        suffix.Bytes.Span.CopyTo(combined.AsSpan(compressed.PrefixBytes.Length));
        return new DatabaseKey(combined);
    }

    #endregion
}

public readonly record struct CompressedKeys(
    int PrefixLength,
    DatabaseKey[] Suffixes,
    byte[] PrefixBytes)
{
    public CompressedKeys(int prefixLength, DatabaseKey[] suffixes)
        : this(prefixLength, suffixes, [])
    {
    }

    public int KeyCount => Suffixes.Length;

    public int EstimatedSavedBytes => PrefixLength * Suffixes.Length;
}

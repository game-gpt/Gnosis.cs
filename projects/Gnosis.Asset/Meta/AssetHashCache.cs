using System.Collections.Concurrent;

namespace Gnosis.Asset.Meta;

public sealed class AssetHashCache
{
    private readonly ConcurrentDictionary<string, AssetHash> _cache = new();

    public int Count => _cache.Count;

    public AssetHash GetOrCompute(string path, Func<Stream> streamFactory)
    {
        var normalizedPath = path.Replace('\\', '/');

        if (_cache.TryGetValue(normalizedPath, out var cachedHash))
        {
            return cachedHash;
        }

        var stream = streamFactory();
        try
        {
            var hash = AssetHashCalculator.ComputeHash(stream);
            _cache[normalizedPath] = hash;
            return hash;
        }
        finally
        {
            stream.Dispose();
        }
    }

    public void SetHash(string path, AssetHash hash)
    {
        var normalizedPath = path.Replace('\\', '/');
        _cache[normalizedPath] = hash;
    }

    public bool Invalidate(string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        return _cache.TryRemove(normalizedPath, out _);
    }

    public void Clear()
    {
        _cache.Clear();
    }

    public bool TryGetHash(string path, out AssetHash hash)
    {
        var normalizedPath = path.Replace('\\', '/');
        return _cache.TryGetValue(normalizedPath, out hash);
    }
}

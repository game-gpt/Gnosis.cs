using System.Collections.Concurrent;

namespace Gnosis.Toolchain.ScriptCompiler.Cache;

public class InMemoryCompilationCache : ICompilationCache
{
    #region Fields

    private readonly ConcurrentDictionary<string, byte[]> _cache = new();

    #endregion

    #region Public Methods

    public bool TryGet(string key, out byte[]? data)
    {
        return _cache.TryGetValue(key, out data);
    }

    public void Set(string key, byte[] data)
    {
        _cache[key] = data;
    }

    public void Invalidate(string key)
    {
        _cache.TryRemove(key, out _);
    }

    public void InvalidateAll()
    {
        _cache.Clear();
    }

    public string ComputeKey(string filePath, string content, string macrosHash)
    {
        return CacheKeyGenerator.Generate(filePath, content, macrosHash);
    }

    #endregion
}

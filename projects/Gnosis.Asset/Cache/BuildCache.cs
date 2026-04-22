using System.Collections.Concurrent;
using System.Text.Json;
using Gnosis.Asset.Meta;

namespace Gnosis.Asset.Cache;

public sealed class BuildCache : IDisposable
{
    private readonly ConcurrentDictionary<string, BuildCacheEntry> _entries = new();
    private readonly string _cacheDirectory;
    private readonly AssetHashCache _hashCache;
    private readonly object _fileLock = new();
    private bool _dirty;
    private bool _disposed;

    public int Count => _entries.Count;
    public string CacheDirectory => _cacheDirectory;

    public BuildCache(string cacheDirectory)
    {
        _cacheDirectory = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));
        _hashCache = new AssetHashCache();

        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }

        LoadFromDisk();
    }

    #region 查询

    /// <summary>
    /// 检查资产是否为最新（源哈希与缓存匹配）
    /// </summary>
    public bool IsUpToDate(string assetPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = NormalizePath(assetPath);

        if (!_entries.TryGetValue(normalizedPath, out var entry))
        {
            return false;
        }

        var currentHash = ComputeFileHash(normalizedPath);
        return currentHash.Equals(entry.SourceHash);
    }

    /// <summary>
    /// 检查资产是否需要重新构建
    /// </summary>
    public bool ShouldRebuild(string assetPath)
    {
        return !IsUpToDate(assetPath);
    }

    /// <summary>
    /// 获取资产的缓存条目
    /// </summary>
    public BuildCacheEntry? GetEntry(string assetPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = NormalizePath(assetPath);
        return _entries.TryGetValue(normalizedPath, out var entry) ? entry : null;
    }

    /// <summary>
    /// 获取资产的输出路径
    /// </summary>
    public string? GetOutputPath(string assetPath)
    {
        var entry = GetEntry(assetPath);
        return entry?.OutputPath;
    }

    /// <summary>
    /// 获取所有缓存条目
    /// </summary>
    public IReadOnlyDictionary<string, BuildCacheEntry> GetAllEntries()
    {
        return new Dictionary<string, BuildCacheEntry>(_entries);
    }

    #endregion

    #region 更新

    /// <summary>
    /// 标记资产为已构建，更新缓存条目
    /// </summary>
    public void MarkBuilt(string assetPath, string outputPath, IReadOnlyList<string>? dependencies = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = NormalizePath(assetPath);
        var sourceHash = ComputeFileHash(normalizedPath);

        var entry = new BuildCacheEntry
        {
            AssetPath = normalizedPath,
            OutputPath = outputPath,
            SourceHash = sourceHash,
            Dependencies = dependencies ?? [],
            BuildTime = DateTime.UtcNow
        };

        _entries[normalizedPath] = entry;
        _dirty = true;
    }

    /// <summary>
    /// 批量标记资产为已构建
    /// </summary>
    public void MarkBuiltBatch(IEnumerable<(string AssetPath, string OutputPath, IReadOnlyList<string>? Dependencies)> items)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var (assetPath, outputPath, dependencies) in items)
        {
            MarkBuilt(assetPath, outputPath, dependencies);
        }
    }

    /// <summary>
    /// 使指定资产的缓存失效
    /// </summary>
    public void Invalidate(string assetPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = NormalizePath(assetPath);
        _entries.TryRemove(normalizedPath, out _);
        _hashCache.Invalidate(normalizedPath);
        _dirty = true;
    }

    /// <summary>
    /// 使所有依赖指定资产的缓存失效（反向依赖传播）
    /// </summary>
    public void InvalidateDependents(string assetPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var normalizedPath = NormalizePath(assetPath);
        var toInvalidate = new List<string>();

        foreach (var (path, entry) in _entries)
        {
            foreach (var dep in entry.Dependencies)
            {
                if (string.Equals(dep, normalizedPath, StringComparison.OrdinalIgnoreCase))
                {
                    toInvalidate.Add(path);
                    break;
                }
            }
        }

        foreach (var path in toInvalidate)
        {
            Invalidate(path);
        }
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _entries.Clear();
        _hashCache.Clear();
        _dirty = true;
    }

    #endregion

    #region 持久化

    /// <summary>
    /// 将缓存保存到磁盘
    /// </summary>
    public void Save()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_dirty)
        {
            return;
        }

        lock (_fileLock)
        {
            var cacheFilePath = Path.Combine(_cacheDirectory, "build_cache.json");
            var data = new BuildCacheData
            {
                Version = 1,
                Entries = _entries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(cacheFilePath, json);
            _dirty = false;
        }
    }

    /// <summary>
    /// 异步保存缓存到磁盘
    /// </summary>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_dirty)
        {
            return;
        }

        var cacheFilePath = Path.Combine(_cacheDirectory, "build_cache.json");
        var data = new BuildCacheData
        {
            Version = 1,
            Entries = _entries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(cacheFilePath, json, cancellationToken);
        _dirty = false;
    }

    /// <summary>
    /// 从磁盘加载缓存
    /// </summary>
    public void Reload()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _entries.Clear();
        _hashCache.Clear();
        LoadFromDisk();
    }

    #endregion

    #region 增量构建

    /// <summary>
    /// 获取需要重新构建的资产列表
    /// </summary>
    public IReadOnlyList<string> GetOutdatedAssets(IEnumerable<string> allAssetPaths)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var outdated = new List<string>();

        foreach (var path in allAssetPaths)
        {
            if (ShouldRebuild(path))
            {
                outdated.Add(path);
            }
        }

        return outdated;
    }

    /// <summary>
    /// 获取需要重新构建的资产列表（含依赖传播）
    /// </summary>
    public IReadOnlyList<string> GetOutdatedAssetsWithDependents(IEnumerable<string> allAssetPaths)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var outdated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in allAssetPaths)
        {
            if (ShouldRebuild(path))
            {
                outdated.Add(path);
                CollectDependents(path, outdated, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }
        }

        return outdated.ToList();
    }

    /// <summary>
    /// 删除过期的缓存输出文件
    /// </summary>
    public int PruneStaleOutputs(IEnumerable<string> currentAssetPaths)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var currentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in currentAssetPaths)
        {
            currentSet.Add(NormalizePath(path));
        }

        var removed = 0;
        var toRemove = new List<string>();

        foreach (var (path, entry) in _entries)
        {
            if (!currentSet.Contains(path))
            {
                toRemove.Add(path);
            }
        }

        foreach (var path in toRemove)
        {
            if (_entries.TryRemove(path, out var entry))
            {
                if (entry.OutputPath != null && File.Exists(entry.OutputPath))
                {
                    try
                    {
                        File.Delete(entry.OutputPath);
                    }
                    catch (IOException)
                    {
                    }
                }

                removed++;
            }
        }

        if (removed > 0)
        {
            _dirty = true;
        }

        return removed;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_dirty)
        {
            try
            {
                Save();
            }
            catch (IOException)
            {
            }
        }

        _entries.Clear();
        _hashCache.Clear();
        _disposed = true;
    }

    #endregion

    #region 私有方法

    private void LoadFromDisk()
    {
        var cacheFilePath = Path.Combine(_cacheDirectory, "build_cache.json");

        if (!File.Exists(cacheFilePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(cacheFilePath);
            var data = JsonSerializer.Deserialize<BuildCacheData>(json);

            if (data?.Entries == null)
            {
                return;
            }

            foreach (var (path, entry) in data.Entries)
            {
                _entries[path] = entry;
            }
        }
        catch (JsonException)
        {
        }
        catch (IOException)
        {
        }
    }

    private AssetHash ComputeFileHash(string path)
    {
        return _hashCache.GetOrCompute(path, () =>
        {
            if (!File.Exists(path))
            {
                return new MemoryStream(Array.Empty<byte>());
            }

            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192);
        });
    }

    private void CollectDependents(string assetPath, HashSet<string> result, HashSet<string> visited)
    {
        if (!visited.Add(assetPath))
        {
            return;
        }

        foreach (var (path, entry) in _entries)
        {
            foreach (var dep in entry.Dependencies)
            {
                if (string.Equals(dep, assetPath, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(path);
                    CollectDependents(path, result, visited);
                    break;
                }
            }
        }
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    #endregion
}

public sealed class BuildCacheEntry
{
    public string AssetPath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public AssetHash SourceHash { get; init; } = AssetHash.Empty;
    public IReadOnlyList<string> Dependencies { get; init; } = [];
    public DateTime BuildTime { get; init; }
}

internal class BuildCacheData
{
    public int Version { get; init; }
    public Dictionary<string, BuildCacheEntry> Entries { get; init; } = new();
}

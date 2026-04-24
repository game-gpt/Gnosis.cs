using GnosisDatabaseCore = Gnosis.Database.Core;

namespace Gnosis.Database.Editor;

public sealed class AssetMetadataStore
{
    #region 字段

    private readonly GnosisDatabaseCore.IKvDatabase _database;
    private readonly EditorCache _cache;
    private readonly string _keyPrefix;

    #endregion

    #region 构造函数

    public AssetMetadataStore(GnosisDatabaseCore.IKvDatabase database, EditorCache? cache = null,
        string keyPrefix = "asset:")
    {
        _database = database;
        _cache = cache ?? new EditorCache(database, new EditorCacheOptions
        {
            HotKeyTtl = TimeSpan.FromMinutes(10),
            MaxHotEntries = 50000,
            MaxWarmEntries = 200000,
            EnablePreload = true,
            PreloadPrefixes = [keyPrefix]
        });
        _keyPrefix = keyPrefix;
    }

    #endregion

    #region 读取

    public async ValueTask<AssetMetadata?> GetAsync(string assetPath, CancellationToken ct = default)
    {
        var key = BuildKey(assetPath);
        var value = await _cache.GetAsync(key, ct);

        if (value.IsEmpty)
        {
            return null;
        }

        return value.ToObject<AssetMetadata>();
    }

    public async ValueTask<IReadOnlyList<AssetMetadata>> QueryByPrefixAsync(string pathPrefix,
        int limit = 1000, CancellationToken ct = default)
    {
        var prefix = GnosisDatabaseCore.DatabaseKey.FromString($"{_keyPrefix}{pathPrefix}");
        var cursor = _database.Seek(prefix);
        var results = new List<AssetMetadata>(Math.Min(limit, 256));
        var count = 0;

        while (cursor.MoveNext() && count < limit)
        {
            var entry = cursor.Current;
            var meta = entry.Value.ToObject<AssetMetadata>();
            if (meta is not null)
            {
                results.Add(meta);
                count++;
            }
        }

        cursor.Dispose();
        return results.AsReadOnly();
    }

    public async ValueTask<IReadOnlyList<AssetMetadata>> QueryByTypeAsync(string assetType,
        int limit = 1000, CancellationToken ct = default)
    {
        var prefix = GnosisDatabaseCore.DatabaseKey.FromString($"{_keyPrefix}type:{assetType}/");
        var cursor = _database.Seek(prefix);
        var results = new List<AssetMetadata>(Math.Min(limit, 256));
        var count = 0;

        while (cursor.MoveNext() && count < limit)
        {
            var entry = cursor.Current;
            var meta = entry.Value.ToObject<AssetMetadata>();
            if (meta is not null)
            {
                results.Add(meta);
                count++;
            }
        }

        cursor.Dispose();
        return results.AsReadOnly();
    }

    #endregion

    #region 写入

    public async ValueTask PutAsync(AssetMetadata metadata, CancellationToken ct = default)
    {
        var key = BuildKey(metadata.Path);
        var value = GnosisDatabaseCore.DatabaseValue.FromObject(metadata);
        await _cache.PutAsync(key, value, ct);
    }

    public async ValueTask<bool> DeleteAsync(string assetPath, CancellationToken ct = default)
    {
        var key = BuildKey(assetPath);
        return await _cache.DeleteAsync(key, ct);
    }

    public async ValueTask<int> DeleteByPrefixAsync(string pathPrefix, CancellationToken ct = default)
    {
        var prefix = GnosisDatabaseCore.DatabaseKey.FromString($"{_keyPrefix}{pathPrefix}");
        var cursor = _database.Seek(prefix);
        var deleted = 0;

        while (cursor.MoveNext())
        {
            var key = cursor.Current.Key;
            if (await _database.DeleteAsync(key, ct))
            {
                deleted++;
            }
        }

        cursor.Dispose();

        await _cache.InvalidatePrefixAsync(prefix);
        return deleted;
    }

    #endregion

    #region 统计

    public async ValueTask<int> CountByPrefixAsync(string pathPrefix, CancellationToken ct = default)
    {
        var prefix = GnosisDatabaseCore.DatabaseKey.FromString($"{_keyPrefix}{pathPrefix}");
        var cursor = _database.Seek(prefix);
        var count = 0;

        while (cursor.MoveNext())
        {
            count++;
        }

        cursor.Dispose();
        return count;
    }

    #endregion

    #region 预加载

    public async ValueTask PreloadAsync(CancellationToken ct = default)
    {
        await _cache.PreloadAsync(ct);
    }

    #endregion

    #region 内部方法

    private GnosisDatabaseCore.DatabaseKey BuildKey(string assetPath)
    {
        return GnosisDatabaseCore.DatabaseKey.FromString($"{_keyPrefix}{assetPath}");
    }

    #endregion

    #region 数据类型

    public sealed class AssetMetadata
    {
        public string Path { get; set; } = "";

        public string AssetType { get; set; } = "";

        public long FileSize { get; set; }

        public DateTimeOffset LastModified { get; set; }

        public string Checksum { get; set; } = "";

        public Dictionary<string, string> Properties { get; set; } = new();
    }

    #endregion
}

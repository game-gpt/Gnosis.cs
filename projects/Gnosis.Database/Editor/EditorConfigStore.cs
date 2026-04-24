using GnosisDatabaseCore = Gnosis.Database.Core;

namespace Gnosis.Database.Editor;

public sealed class EditorConfigStore
{
    #region 字段

    private readonly GnosisDatabaseCore.IKvDatabase _database;
    private readonly EditorCache _cache;
    private readonly string _keyPrefix;

    #endregion

    #region 事件

    public event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

    #endregion

    #region 构造函数

    public EditorConfigStore(GnosisDatabaseCore.IKvDatabase database, EditorCache? cache = null,
        string keyPrefix = "config:")
    {
        _database = database;
        _cache = cache ?? new EditorCache(database, new EditorCacheOptions
        {
            HotKeyTtl = TimeSpan.FromMinutes(5),
            MaxHotEntries = 10000,
            MaxWarmEntries = 50000,
            EnablePreload = true,
            PreloadPrefixes = [keyPrefix]
        });
        _keyPrefix = keyPrefix;
    }

    #endregion

    #region 读取

    public async ValueTask<string?> GetStringAsync(string configKey, CancellationToken ct = default)
    {
        var key = BuildKey(configKey);
        var value = await _cache.GetAsync(key, ct);
        return value.IsEmpty ? null : value.ToObject<string>();
    }

    public async ValueTask<T?> GetAsync<T>(string configKey, CancellationToken ct = default) where T : class
    {
        var key = BuildKey(configKey);
        var value = await _cache.GetAsync(key, ct);
        return value.IsEmpty ? null : value.ToObject<T>();
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> GetAllAsync(string configGroup,
        CancellationToken ct = default)
    {
        var prefix = GnosisDatabaseCore.DatabaseKey.FromString($"{_keyPrefix}{configGroup}:");
        var cursor = _database.Seek(prefix);
        var results = new Dictionary<string, string>();

        while (cursor.MoveNext())
        {
            var entry = cursor.Current;
            var keyStr = entry.Key.ToString();
            var valueStr = entry.Value.ToObject<string>();
            if (keyStr is not null && valueStr is not null)
            {
                results[keyStr] = valueStr;
            }
        }

        cursor.Dispose();
        return results;
    }

    #endregion

    #region 写入

    public async ValueTask SetAsync(string configKey, string value, CancellationToken ct = default)
    {
        var key = BuildKey(configKey);
        var dbValue = GnosisDatabaseCore.DatabaseValue.FromString(value);
        await _cache.PutAsync(key, dbValue, ct);
        OnConfigChanged(configKey, value);
    }

    public async ValueTask SetAsync<T>(string configKey, T value, CancellationToken ct = default) where T : class
    {
        var key = BuildKey(configKey);
        var dbValue = GnosisDatabaseCore.DatabaseValue.FromObject(value);
        await _cache.PutAsync(key, dbValue, ct);
        OnConfigChanged(configKey, value);
    }

    public async ValueTask SetBatchAsync(IEnumerable<KeyValuePair<string, string>> entries,
        CancellationToken ct = default)
    {
        using var tx = _database.BeginTransaction();

        foreach (var (configKey, value) in entries)
        {
            var key = BuildKey(configKey);
            var dbValue = GnosisDatabaseCore.DatabaseValue.FromString(value);
            await tx.PutAsync(key, dbValue, ct);
        }

        await tx.CommitAsync(ct);

        foreach (var (configKey, value) in entries)
        {
            var key = BuildKey(configKey);
            var dbValue = GnosisDatabaseCore.DatabaseValue.FromString(value);
            await _cache.PutAsync(key, dbValue, ct);
            OnConfigChanged(configKey, value);
        }
    }

    #endregion

    #region 删除

    public async ValueTask<bool> DeleteAsync(string configKey, CancellationToken ct = default)
    {
        var key = BuildKey(configKey);
        var deleted = await _cache.DeleteAsync(key, ct);

        if (deleted)
        {
            OnConfigChanged(configKey, null);
        }

        return deleted;
    }

    public async ValueTask InvalidateGroupAsync(string configGroup, CancellationToken ct = default)
    {
        var prefix = GnosisDatabaseCore.DatabaseKey.FromString($"{_keyPrefix}{configGroup}:");
        await _cache.InvalidatePrefixAsync(prefix, ct);
    }

    #endregion

    #region 预加载

    public async ValueTask PreloadAsync(CancellationToken ct = default)
    {
        await _cache.PreloadAsync(ct);
    }

    #endregion

    #region 内部方法

    private GnosisDatabaseCore.DatabaseKey BuildKey(string configKey)
    {
        return GnosisDatabaseCore.DatabaseKey.FromString($"{_keyPrefix}{configKey}");
    }

    private void OnConfigChanged(string key, object? value)
    {
        ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(key, value));
    }

    #endregion

    #region 事件参数

    public sealed class ConfigChangedEventArgs : EventArgs
    {
        public string Key { get; }

        public object? Value { get; }

        public ConfigChangedEventArgs(string key, object? value)
        {
            Key = key;
            Value = value;
        }
    }

    #endregion
}

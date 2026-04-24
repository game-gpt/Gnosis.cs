namespace Gnosis.Database.Editor;

public sealed class ConfigSyncService
{
    #region 字段

    private readonly Core.IKvDatabase _database;
    private readonly EditorCache _cache;
    private readonly string _configPrefix;
    private readonly List<ConfigChangeHandler> _handlers;
    private readonly SemaphoreSlim _lock = new(1, 1);

    #endregion

    #region 构造函数

    public ConfigSyncService(Core.IKvDatabase database, EditorCache cache, string configPrefix = "config:")
    {
        _database = database;
        _cache = cache;
        _configPrefix = configPrefix;
        _handlers = new List<ConfigChangeHandler>();
    }

    #endregion

    #region 配置读写

    public async ValueTask<T?> GetConfigAsync<T>(string configKey, CancellationToken ct = default)
    {
        var fullKey = Core.DatabaseKey.FromString($"{_configPrefix}{configKey}");
        var value = await _cache.GetAsync(fullKey, ct);
        if (value.IsEmpty)
        {
            return default;
        }

        return value.ToObject<T>();
    }

    public async ValueTask SetConfigAsync<T>(string configKey, T value, CancellationToken ct = default)
    {
        var fullKey = Core.DatabaseKey.FromString($"{_configPrefix}{configKey}");
        var dbValue = Core.DatabaseValue.FromObject(value);
        await _cache.PutAsync(fullKey, dbValue, ct);

        await NotifyChangeAsync(configKey, ConfigChangeType.Updated, ct);
    }

    public async ValueTask<bool> DeleteConfigAsync(string configKey, CancellationToken ct = default)
    {
        var fullKey = Core.DatabaseKey.FromString($"{_configPrefix}{configKey}");
        var deleted = await _cache.DeleteAsync(fullKey, ct);

        if (deleted)
        {
            await NotifyChangeAsync(configKey, ConfigChangeType.Removed, ct);
        }

        return deleted;
    }

    #endregion

    #region 变更通知

    public void OnChange(ConfigChangeHandler handler)
    {
        _handlers.Add(handler);
    }

    public void RemoveChangeHandler(ConfigChangeHandler handler)
    {
        _handlers.Remove(handler);
    }

    #endregion

    #region 批量同步

    public async ValueTask SyncBatchAsync(Dictionary<string, object> updates, CancellationToken ct = default)
    {
        var changedKeys = new List<string>();

        foreach (var (key, value) in updates)
        {
            var fullKey = Core.DatabaseKey.FromString($"{_configPrefix}{key}");
            var dbValue = Core.DatabaseValue.FromObject(value);
            await _cache.PutAsync(fullKey, dbValue, ct);
            changedKeys.Add(key);
        }

        foreach (var key in changedKeys)
        {
            await NotifyChangeAsync(key, ConfigChangeType.Updated, ct);
        }
    }

    #endregion

    #region 内部方法

    private async ValueTask NotifyChangeAsync(string configKey, ConfigChangeType changeType, CancellationToken ct)
    {
        var args = new ConfigChangeEventArgs(configKey, changeType);

        foreach (var handler in _handlers)
        {
            try
            {
                await handler(args, ct);
            }
            catch
            {
                // 处理器异常不阻断其他处理器
            }
        }
    }

    #endregion

    #region 内部类型

    public delegate Task ConfigChangeHandler(ConfigChangeEventArgs args, CancellationToken ct);

    public sealed class ConfigChangeEventArgs(string configKey, ConfigChangeType changeType)
    {
        public string ConfigKey { get; } = configKey;

        public ConfigChangeType ChangeType { get; } = changeType;

        public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
    }

    public enum ConfigChangeType
    {
        Updated,
        Removed
    }

    #endregion
}

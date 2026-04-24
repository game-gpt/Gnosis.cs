using System.Text.Json;
using Gnosis.Database.Core;
using Gnosis.Core;

namespace Gnosis.Security.AntiCheat;

public sealed class DatabaseViolationStore
{
    #region 常量

    private const string ViolationKeyPrefix = "anticheat:violation:";
    private const string BehaviorKeyPrefix = "anticheat:behavior:";
    private const string StatsKeyPrefix = "anticheat:stats:";

    #endregion

    #region 字段

    private readonly IKvDatabase _database;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly int _maxViolationsPerPlayer;

    #endregion

    #region 构造函数

    public DatabaseViolationStore(IKvDatabase database, int maxViolationsPerPlayer = 1000)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _maxViolationsPerPlayer = maxViolationsPerPlayer;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #endregion

    #region 违规记录持久化

    public async Task StoreViolationAsync(PlayerId playerId, ViolationType violationType, string details, DetectionLevel detectionLevel = DetectionLevel.Warning)
    {
        var record = new StoredViolationRecord
        {
            PlayerId = playerId.Value.ToString(),
            ViolationType = violationType.ToString(),
            Details = details,
            DetectionLevel = detectionLevel.ToString(),
            TimestampMs = Environment.TickCount64
        };

        var key = DatabaseKey.FromString($"{ViolationKeyPrefix}{playerId.Value}:{record.TimestampMs}");
        var json = JsonSerializer.Serialize(record, _jsonOptions);
        var value = new DatabaseValue(System.Text.Encoding.UTF8.GetBytes(json));
        await _database.PutAsync(key, value);

        await IncrementViolationCountAsync(playerId, violationType);
    }

    public async Task StoreViolationBatchAsync(IEnumerable<ReportEvent> events)
    {
        foreach (var evt in events)
        {
            await StoreViolationAsync(evt.PlayerId, evt.ViolationType, evt.Details, evt.DetectionLevel);
        }
    }

    public async Task<List<StoredViolationRecord>> GetViolationHistoryAsync(PlayerId playerId, int limit = 100)
    {
        var prefix = DatabaseKey.FromString($"{ViolationKeyPrefix}{playerId.Value}:");
        var results = new List<StoredViolationRecord>();

        using var cursor = _database.Seek(prefix);

        while (cursor.MoveNext() && results.Count < limit)
        {
            var json = System.Text.Encoding.UTF8.GetString(cursor.Current.Value.Bytes.Span);

            try
            {
                var record = JsonSerializer.Deserialize<StoredViolationRecord>(json, _jsonOptions);
                if (record != null)
                {
                    results.Add(record);
                }
            }
            catch (JsonException)
            {
                continue;
            }
        }

        return results;
    }

    public async Task<int> GetViolationCountAsync(PlayerId playerId)
    {
        var statsKey = DatabaseKey.FromString($"{StatsKeyPrefix}{playerId.Value}:total");
        var statsValue = await _database.GetAsync(statsKey);

        if (statsValue is not { IsEmpty: false })
        {
            return 0;
        }

        var json = System.Text.Encoding.UTF8.GetString(statsValue.Value.Bytes.Span);

        try
        {
            var stats = JsonSerializer.Deserialize<PlayerViolationStats>(json, _jsonOptions);
            return stats?.TotalCount ?? 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    #endregion

    #region 行为记录持久化

    public async Task StoreBehaviorAsync(PlayerId playerId, string actionType, string details)
    {
        var record = new StoredBehaviorRecord
        {
            PlayerId = playerId.Value.ToString(),
            ActionType = actionType,
            Details = details,
            TimestampMs = Environment.TickCount64
        };

        var key = DatabaseKey.FromString($"{BehaviorKeyPrefix}{playerId.Value}:{record.TimestampMs}");
        var json = JsonSerializer.Serialize(record, _jsonOptions);
        var value = new DatabaseValue(System.Text.Encoding.UTF8.GetBytes(json));
        await _database.PutAsync(key, value);
    }

    public async Task<List<StoredBehaviorRecord>> GetBehaviorHistoryAsync(PlayerId playerId, int limit = 100)
    {
        var prefix = DatabaseKey.FromString($"{BehaviorKeyPrefix}{playerId.Value}:");
        var results = new List<StoredBehaviorRecord>();

        using var cursor = _database.Seek(prefix);

        while (cursor.MoveNext() && results.Count < limit)
        {
            var json = System.Text.Encoding.UTF8.GetString(cursor.Current.Value.Bytes.Span);

            try
            {
                var record = JsonSerializer.Deserialize<StoredBehaviorRecord>(json, _jsonOptions);
                if (record != null)
                {
                    results.Add(record);
                }
            }
            catch (JsonException)
            {
                continue;
            }
        }

        return results;
    }

    #endregion

    #region 清理

    public async Task ClearPlayerDataAsync(PlayerId playerId)
    {
        await ClearKeysWithPrefixAsync($"{ViolationKeyPrefix}{playerId.Value}:");
        await ClearKeysWithPrefixAsync($"{BehaviorKeyPrefix}{playerId.Value}:");
        await ClearKeysWithPrefixAsync($"{StatsKeyPrefix}{playerId.Value}:");
    }

    #endregion

    #region 私有方法

    private async Task IncrementViolationCountAsync(PlayerId playerId, ViolationType violationType)
    {
        var statsKey = DatabaseKey.FromString($"{StatsKeyPrefix}{playerId.Value}:total");
        var statsValue = await _database.GetAsync(statsKey);

        PlayerViolationStats stats;

        if (statsValue is { IsEmpty: false })
        {
            var json = System.Text.Encoding.UTF8.GetString(statsValue.Value.Bytes.Span);

            try
            {
                stats = JsonSerializer.Deserialize<PlayerViolationStats>(json, _jsonOptions) ?? new PlayerViolationStats();
            }
            catch (JsonException)
            {
                stats = new PlayerViolationStats();
            }
        }
        else
        {
            stats = new PlayerViolationStats();
        }

        stats.TotalCount++;
        stats.LastViolationType = violationType.ToString();
        stats.LastViolationTimestampMs = Environment.TickCount64;

        if (!stats.CountByType.ContainsKey(violationType.ToString()))
        {
            stats.CountByType[violationType.ToString()] = 0;
        }

        stats.CountByType[violationType.ToString()]++;

        var updatedJson = JsonSerializer.Serialize(stats, _jsonOptions);
        var updatedValue = new DatabaseValue(System.Text.Encoding.UTF8.GetBytes(updatedJson));
        await _database.PutAsync(statsKey, updatedValue);
    }

    private async Task ClearKeysWithPrefixAsync(string prefix)
    {
        var dbPrefix = DatabaseKey.FromString(prefix);
        var keysToDelete = new List<DatabaseKey>();

        using var cursor = _database.Seek(dbPrefix);

        while (cursor.MoveNext())
        {
            keysToDelete.Add(cursor.Current.Key);
        }

        foreach (var key in keysToDelete)
        {
            await _database.DeleteAsync(key);
        }
    }

    #endregion
}

public sealed class StoredViolationRecord
{
    public string PlayerId { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string DetectionLevel { get; set; } = string.Empty;
    public long TimestampMs { get; set; }
}

public sealed class StoredBehaviorRecord
{
    public string PlayerId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public long TimestampMs { get; set; }
}

public sealed class PlayerViolationStats
{
    public int TotalCount { get; set; }
    public string LastViolationType { get; set; } = string.Empty;
    public long LastViolationTimestampMs { get; set; }
    public Dictionary<string, int> CountByType { get; set; } = new();
}

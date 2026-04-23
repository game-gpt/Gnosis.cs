using System.Text.Json;
using Gnosis.Database.Core;
using Gnosis.ECS.World;

namespace Gnosis.Storage.Bridge;

public sealed class WorldDataBridge
{
    #region 常量

    private const string WorldKeyPrefix = "world:";
    private const string EntityKeyPrefix = "world:entity:";
    private const string ComponentKeyPrefix = "world:component:";
    private const string MetaKey = "world:meta";

    #endregion

    #region 字段

    private readonly IKvDatabase _database;
    private readonly WorldSerializer _serializer;
    private readonly JsonSerializerOptions _jsonOptions;

    #endregion

    #region 构造函数

    public WorldDataBridge(IKvDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _serializer = new WorldSerializer();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #endregion

    #region 世界持久化

    public async Task SaveWorldAsync(World world, string worldId = "default")
    {
        var snapshot = _serializer.Serialize(world);
        await PersistSnapshotAsync(snapshot, worldId);
    }

    public async Task<WorldSnapshot?> LoadWorldSnapshotAsync(string worldId = "default")
    {
        var metaKey = DatabaseKey.FromString($"{WorldKeyPrefix}{worldId}:meta");

        var metaValue = await _database.GetAsync(metaKey);
        if (metaValue == null || metaValue.IsEmpty)
        {
            return null;
        }

        var metaJson = System.Text.Encoding.UTF8.GetString(metaValue.Bytes.Span);
        var meta = JsonSerializer.Deserialize<WorldMeta>(metaJson, _jsonOptions);

        if (meta == null)
        {
            return null;
        }

        var snapshot = new WorldSnapshot
        {
            Timestamp = meta.Timestamp,
            Version = meta.Version
        };

        for (uint i = 0; i < meta.EntityCount; i++)
        {
            var entityKey = DatabaseKey.FromString($"{WorldKeyPrefix}{worldId}:entity:{i}");
            var entityValue = await _database.GetAsync(entityKey);

            if (entityValue == null || entityValue.IsEmpty)
            {
                continue;
            }

            var entityJson = System.Text.Encoding.UTF8.GetString(entityValue.Bytes.Span);
            var entitySnapshot = JsonSerializer.Deserialize<EntitySnapshot>(entityJson, _jsonOptions);

            if (entitySnapshot != null)
            {
                snapshot.Entities.Add(entitySnapshot);
            }
        }

        return snapshot;
    }

    public async Task RestoreWorldAsync(World world, string worldId = "default")
    {
        var snapshot = await LoadWorldSnapshotAsync(worldId);

        if (snapshot == null)
        {
            return;
        }

        _serializer.Deserialize(world, snapshot);
    }

    #endregion

    #region 运行时数据查询

    public async Task<EntitySnapshot?> GetEntitySnapshotAsync(string worldId, uint entityIndex, uint entityGeneration)
    {
        var entityKey = DatabaseKey.FromString($"{WorldKeyPrefix}{worldId}:entity:{entityIndex}");
        var entityValue = await _database.GetAsync(entityKey);

        if (entityValue == null || entityValue.IsEmpty)
        {
            return null;
        }

        var entityJson = System.Text.Encoding.UTF8.GetString(entityValue.Bytes.Span);
        return JsonSerializer.Deserialize<EntitySnapshot>(entityJson, _jsonOptions);
    }

    public async Task<List<EntitySnapshot>> QueryEntitiesByComponentAsync(string worldId, string componentTypeName)
    {
        var results = new List<EntitySnapshot>();
        var metaKey = DatabaseKey.FromString($"{WorldKeyPrefix}{worldId}:meta");
        var metaValue = await _database.GetAsync(metaKey);

        if (metaValue == null || metaValue.IsEmpty)
        {
            return results;
        }

        var metaJson = System.Text.Encoding.UTF8.GetString(metaValue.Bytes.Span);
        var meta = JsonSerializer.Deserialize<WorldMeta>(metaJson, _jsonOptions);

        if (meta == null)
        {
            return results;
        }

        for (uint i = 0; i < meta.EntityCount; i++)
        {
            var entity = await GetEntitySnapshotAsync(worldId, i, 0);

            if (entity != null && entity.Components.Any(c => c.TypeName.Contains(componentTypeName)))
            {
                results.Add(entity);
            }
        }

        return results;
    }

    public async Task<ComponentSnapshot?> GetComponentSnapshotAsync(string worldId, uint entityIndex, string componentTypeName)
    {
        var entity = await GetEntitySnapshotAsync(worldId, entityIndex, 0);
        return entity?.Components.FirstOrDefault(c => c.TypeName.Contains(componentTypeName));
    }

    #endregion

    #region 私有方法

    private async Task PersistSnapshotAsync(WorldSnapshot snapshot, string worldId)
    {
        var meta = new WorldMeta
        {
            WorldId = worldId,
            Timestamp = snapshot.Timestamp,
            Version = snapshot.Version,
            EntityCount = (uint)snapshot.Entities.Count
        };

        var metaJson = JsonSerializer.Serialize(meta, _jsonOptions);
        var metaBytes = System.Text.Encoding.UTF8.GetBytes(metaJson);
        var metaKey = DatabaseKey.FromString($"{WorldKeyPrefix}{worldId}:meta");
        await _database.PutAsync(metaKey, new DatabaseValue(metaBytes));

        for (var i = 0; i < snapshot.Entities.Count; i++)
        {
            var entity = snapshot.Entities[i];
            var entityJson = JsonSerializer.Serialize(entity, _jsonOptions);
            var entityBytes = System.Text.Encoding.UTF8.GetBytes(entityJson);
            var entityKey = DatabaseKey.FromString($"{WorldKeyPrefix}{worldId}:entity:{entity.Index}");
            await _database.PutAsync(entityKey, new DatabaseValue(entityBytes));
        }
    }

    #endregion

    #region 内部类型

    private sealed class WorldMeta
    {
        public string WorldId { get; set; } = "default";
        public long Timestamp { get; set; }
        public int Version { get; set; } = 1;
        public uint EntityCount { get; set; }
    }

    #endregion
}

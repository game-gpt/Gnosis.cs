using System.Text.Json;
using Gnosis.Database.Core;
using Gnosis.ECS.Component;
using Gnosis.ECS.World;
using Gnosis.Security.Encryption;

namespace Gnosis.Storage.Integration;

public sealed class SecureWorldDataBridge
{
    #region 常量

    private const string WorldKeyPrefix = "world:";
    private const string MetaKey = "world:meta";

    #endregion

    #region 字段

    private readonly IKvDatabase _database;
    private readonly WorldSerializer _serializer;
    private readonly EncryptedComponentProcessor _encryptionProcessor;
    private readonly ComponentSerializer _componentSerializer;
    private readonly JsonSerializerOptions _jsonOptions;

    #endregion

    #region 构造函数

    public SecureWorldDataBridge(IKvDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _serializer = new WorldSerializer();
        _encryptionProcessor = new EncryptedComponentProcessor();
        _componentSerializer = new ComponentSerializer();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public SecureWorldDataBridge(IKvDatabase database, EncryptedComponentProcessor encryptionProcessor)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _serializer = new WorldSerializer();
        _encryptionProcessor = encryptionProcessor ?? throw new ArgumentNullException(nameof(encryptionProcessor));
        _componentSerializer = new ComponentSerializer();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #endregion

    #region 属性

    public EncryptedComponentProcessor EncryptionProcessor => _encryptionProcessor;

    #endregion

    #region 世界持久化

    public async Task SaveWorldAsync(World world, string worldId = "default")
    {
        var snapshot = _serializer.Serialize(world);
        await PersistSecureSnapshotAsync(snapshot, worldId);
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
                DecryptEntityComponents(entitySnapshot);
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

    #region 私有方法

    private async Task PersistSecureSnapshotAsync(WorldSnapshot snapshot, string worldId)
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
            EncryptEntityComponents(entity);

            var entityJson = JsonSerializer.Serialize(entity, _jsonOptions);
            var entityBytes = System.Text.Encoding.UTF8.GetBytes(entityJson);
            var entityKey = DatabaseKey.FromString($"{WorldKeyPrefix}{worldId}:entity:{entity.Index}");
            await _database.PutAsync(entityKey, new DatabaseValue(entityBytes));
        }
    }

    private void EncryptEntityComponents(EntitySnapshot entity)
    {
        foreach (var component in entity.Components)
        {
            var componentType = Type.GetType(component.TypeName);

            if (componentType == null)
            {
                continue;
            }

            if (_encryptionProcessor.HasEncryptedFields(componentType))
            {
                component.JsonData = _encryptionProcessor.ProcessSerialize(
                    component.JsonData, componentType, _componentSerializer);
            }
        }
    }

    private void DecryptEntityComponents(EntitySnapshot entity)
    {
        foreach (var component in entity.Components)
        {
            var componentType = Type.GetType(component.TypeName);

            if (componentType == null)
            {
                continue;
            }

            if (_encryptionProcessor.HasEncryptedFields(componentType))
            {
                try
                {
                    component.JsonData = _encryptionProcessor.ProcessDeserialize(
                        component.JsonData, componentType, _componentSerializer);
                }
                catch (SecurityException)
                {
                    component.JsonData = "{}";
                }
            }
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

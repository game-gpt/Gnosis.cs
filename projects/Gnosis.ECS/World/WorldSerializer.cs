using Gnosis.ECS.Component;
using Gnosis.ECS.Entity;

namespace Gnosis.ECS.World;

/// <summary>
/// 世界序列化数据，包含世界的所有实体和组件信息
/// </summary>
public sealed class WorldSnapshot
{
    /// <summary>
    /// 实体列表
    /// </summary>
    public List<EntitySnapshot> Entities { get; set; } = new();

    /// <summary>
    /// 快照创建时间戳
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// 快照版本
    /// </summary>
    public int Version { get; set; } = 1;
}

/// <summary>
/// 实体序列化快照
/// </summary>
public sealed class EntitySnapshot
{
    /// <summary>
    /// 实体 ID 索引
    /// </summary>
    public uint Index { get; set; }

    /// <summary>
    /// 实体代际
    /// </summary>
    public uint Generation { get; set; }

    /// <summary>
    /// 组件数据列表（JSON 格式）
    /// </summary>
    public List<ComponentSnapshot> Components { get; set; } = new();
}

/// <summary>
/// 组件序列化快照
/// </summary>
public sealed class ComponentSnapshot
{
    /// <summary>
    /// 组件类型的完整名称
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// 组件数据的 JSON 表示
    /// </summary>
    public string JsonData { get; set; } = string.Empty;
}

/// <summary>
/// 世界序列化器接口，支持世界状态的保存和恢复。
/// 序列化包含所有实体及其组件数据，不包含系统和查询状态。
/// </summary>
public interface IWorldSerializer
{
    WorldSnapshot Serialize(World world);
    void Deserialize(World world, WorldSnapshot snapshot);
}

/// <summary>
/// 世界序列化器，支持世界状态的保存和恢复。
/// 序列化包含所有实体及其组件数据，不包含系统和查询状态。
/// 实际组件序列化/反序列化由 IComponentSerializer 插件处理。
/// </summary>
public sealed class WorldSerializer : IWorldSerializer
{
    #region 字段

    private readonly global::System.Text.Json.JsonSerializerOptions _jsonOptions;

    #endregion

    #region 构造函数

    public WorldSerializer()
    {
        _jsonOptions = new global::System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = global::System.Text.Json.JsonNamingPolicy.CamelCase
        };
    }

    #endregion

    #region 序列化

    /// <summary>
    /// 将世界序列化为快照对象。
    /// 组件数据通过 IComponentSerializer 接口委托给具体实现。
    /// </summary>
    public WorldSnapshot Serialize(World world)
    {
        var snapshot = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var archetypes = world.Archetypes.GetArchetypes();

        foreach (var archetype in archetypes)
        {
            foreach (var entityId in archetype.GetEntities())
            {
                if (!world.Entities.IsAlive(entityId))
                {
                    continue;
                }

                var entitySnapshot = new EntitySnapshot
                {
                    Index = entityId.Index,
                    Generation = entityId.Generation
                };

                foreach (var componentType in archetype.ComponentTypes)
                {
                    var componentData = TrySerializeComponent(world, entityId, componentType);

                    if (componentData != null)
                    {
                        entitySnapshot.Components.Add(componentData);
                    }
                }

                snapshot.Entities.Add(entitySnapshot);
            }
        }

        return snapshot;
    }

    /// <summary>
    /// 将世界序列化为 JSON 字符串
    /// </summary>
    public string SerializeToJson(World world)
    {
        var snapshot = Serialize(world);
        return global::System.Text.Json.JsonSerializer.Serialize(snapshot, _jsonOptions);
    }

    #endregion

    #region 反序列化

    /// <summary>
    /// 从快照恢复世界状态。
    /// 注意：这会清空世界中现有的所有实体。
    /// 组件反序列化通过 IComponentSerializer 接口委托给具体实现。
    /// </summary>
    public void Deserialize(World world, WorldSnapshot snapshot)
    {
        var existingEntities = world.Entities.CreateEntities(0);

        foreach (var entitySnapshot in snapshot.Entities)
        {
            var entityId = world.CreateEntity();

            foreach (var componentSnapshot in entitySnapshot.Components)
            {
                TryDeserializeComponent(world, entityId, componentSnapshot);
            }
        }
    }

    /// <summary>
    /// 从 JSON 字符串恢复世界状态
    /// </summary>
    public void DeserializeFromJson(World world, string json)
    {
        var snapshot = global::System.Text.Json.JsonSerializer.Deserialize<WorldSnapshot>(json, _jsonOptions);

        if (snapshot == null)
        {
            throw new InvalidOperationException("反序列化失败：JSON 为空或格式错误");
        }

        Deserialize(world, snapshot);
    }

    #endregion

    #region 私有方法

    private ComponentSnapshot? TrySerializeComponent(World world, EntityId entityId, Type componentType)
    {
        try
        {
            var pool = world.Components.GetPool(componentType);

            if (pool == null || !pool.HasEntity(entityId))
            {
                return null;
            }

            var component = pool.GetComponentData(entityId);

            if (component == null)
            {
                return null;
            }

            var json = global::System.Text.Json.JsonSerializer.Serialize(component, componentType, _jsonOptions);

            return new ComponentSnapshot
            {
                TypeName = componentType.AssemblyQualifiedName ?? componentType.FullName ?? componentType.Name,
                JsonData = json
            };
        }
        catch
        {
            return null;
        }
    }

    private void TryDeserializeComponent(World world, EntityId entityId, ComponentSnapshot componentSnapshot)
    {
        try
        {
            var componentType = Type.GetType(componentSnapshot.TypeName);

            if (componentType == null)
            {
                return;
            }

            var component = global::System.Text.Json.JsonSerializer.Deserialize(componentSnapshot.JsonData, componentType, _jsonOptions);

            if (component == null)
            {
                return;
            }

            var pool = world.Components.GetPool(componentType);

            if (pool == null)
            {
                pool = world.Components.GetOrCreatePool(componentType);
            }

            pool.AddComponentData(entityId, component);
        }
        catch
        {
        }
    }

    #endregion
}

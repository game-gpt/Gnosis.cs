using System.Text.Json;
using Gnosis.ECS.Archetype;
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
/// 世界序列化器，支持世界状态的保存和恢复。
/// 序列化包含所有实体及其组件数据，不包含系统和查询状态。
/// </summary>
public sealed class WorldSerializer
{
    #region 字段

    private readonly ComponentSerializer _componentSerializer;
    private readonly JsonSerializerOptions _jsonOptions;

    #endregion

    #region 构造函数

    public WorldSerializer()
    {
        _componentSerializer = new ComponentSerializer();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #endregion

    #region 序列化

    /// <summary>
    /// 将世界序列化为快照对象
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
                    var componentData = SerializeComponent(world, entityId, componentType);

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
        return JsonSerializer.Serialize(snapshot, _jsonOptions);
    }

    #endregion

    #region 反序列化

    /// <summary>
    /// 从快照恢复世界状态。
    /// 注意：这会清空世界中现有的所有实体。
    /// </summary>
    public void Deserialize(World world, WorldSnapshot snapshot)
    {
        // 清空现有实体
        var existingEntities = world.Entities.CreateEntities(0);

        // 重新创建实体和组件
        foreach (var entitySnapshot in snapshot.Entities)
        {
            var entityId = world.CreateEntity();

            foreach (var componentSnapshot in entitySnapshot.Components)
            {
                DeserializeComponent(world, entityId, componentSnapshot);
            }
        }
    }

    /// <summary>
    /// 从 JSON 字符串恢复世界状态
    /// </summary>
    public void DeserializeFromJson(World world, string json)
    {
        var snapshot = JsonSerializer.Deserialize<WorldSnapshot>(json, _jsonOptions);

        if (snapshot == null)
        {
            throw new InvalidOperationException("反序列化失败：JSON 为空或格式错误");
        }

        Deserialize(world, snapshot);
    }

    #endregion

    #region 私有方法

    private ComponentSnapshot? SerializeComponent(World world, EntityId entityId, Type componentType)
    {
        try
        {
            var pool = world.Components.GetPool(componentType);

            if (pool == null || !pool.HasEntity(entityId))
            {
                return null;
            }

            var getMethod = pool.GetType().GetMethod("Get", new[] { typeof(EntityId) });

            if (getMethod == null)
            {
                return null;
            }

            var component = getMethod.Invoke(pool, new object[] { entityId });

            if (component == null)
            {
                return null;
            }

            var json = _componentSerializer.Serialize(component, componentType);

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

    private void DeserializeComponent(World world, EntityId entityId, ComponentSnapshot componentSnapshot)
    {
        try
        {
            var componentType = Type.GetType(componentSnapshot.TypeName);

            if (componentType == null)
            {
                return;
            }

            var component = _componentSerializer.Deserialize(componentSnapshot.JsonData, componentType);
            var addMethod = typeof(World).GetMethod("AddComponent")?.MakeGenericMethod(componentType);

            if (addMethod != null)
            {
                addMethod.Invoke(world, new[] { entityId, component });
            }
        }
        catch
        {
            // 跳过无法反序列化的组件
        }
    }

    #endregion
}

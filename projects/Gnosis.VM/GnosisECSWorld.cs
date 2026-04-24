using Gnosis.Core;
using Gnosis.Core.Entity;
using Gnosis.ECS.Component;
using Gnosis.ECS.World;

namespace Gnosis.VM;

/// <summary>
///     基于 Gnosis.ECS 的游戏世界实现
///     通过组件类型注册表将字符串类型名映射到泛型方法
/// </summary>
public sealed class GnosisECSWorld : IGameWorld
{
    private readonly IWorld _world;
    private readonly Dictionary<string, Type> _componentTypeRegistry;
    private readonly Dictionary<long, EntityId> _entityIdMap;

    /// <summary>
    ///     初始化 GnosisECSWorld
    /// </summary>
    /// <param name="world">Gnosis ECS 世界实例</param>
    public GnosisECSWorld(IWorld world)
    {
        _world = world;
        _componentTypeRegistry = new Dictionary<string, Type>();
        _entityIdMap = new Dictionary<long, EntityId>();
    }

    /// <summary>
    ///     注册组件类型
    /// </summary>
    /// <param name="name">组件类型名称</param>
    /// <typeparam name="T">组件类型</typeparam>
    public void RegisterComponentType<T>(string name) where T : struct
    {
        _componentTypeRegistry[name] = typeof(T);
    }

    /// <inheritdoc />
    public long SpawnEntity()
    {
        var entity = _world.CreateEntity();
        var handle = EncodeEntityId(entity);
        _entityIdMap[handle] = entity;
        return handle;
    }

    /// <inheritdoc />
    public void DestroyEntity(long entityId)
    {
        var eid = DecodeEntityId(entityId);
        _world.DestroyEntity(eid);
        _entityIdMap.Remove(entityId);
    }

    /// <inheritdoc />
    public void AddComponent(long entityId, string componentType, Dictionary<string, object?> fields)
    {
        if (!_componentTypeRegistry.TryGetValue(componentType, out var type)) return;

        var eid = DecodeEntityId(entityId);
        var component = Activator.CreateInstance(type)!;
        ApplyFields(component, type, fields);
        InvokeGenericMethod("AddComponent", type, eid, component);
    }

    /// <inheritdoc />
    public object? GetComponent(long entityId, string componentType, string fieldName)
    {
        if (!_componentTypeRegistry.TryGetValue(componentType, out var type)) return null;

        var eid = DecodeEntityId(entityId);
        var component = InvokeGenericMethod("GetComponent", type, eid);
        if (component is null) return null;

        var prop = type.GetProperty(fieldName);
        return prop?.GetValue(component);
    }

    /// <inheritdoc />
    public void SetComponent(long entityId, string componentType, string fieldName, object? value)
    {
        if (!_componentTypeRegistry.TryGetValue(componentType, out var type)) return;

        var eid = DecodeEntityId(entityId);

        var pool = ((Gnosis.ECS.World.World)_world).Components.GetPool(type);
        if (pool is null) return;

        var componentData = pool.GetComponentData(eid);
        if (componentData is null) return;

        var prop = type.GetProperty(fieldName);
        if (prop is not null && prop.CanWrite)
        {
            prop.SetValue(componentData, value);
            pool.AddComponentData(eid, componentData);
        }
    }

    /// <inheritdoc />
    public void RemoveComponent(long entityId, string componentType)
    {
        if (!_componentTypeRegistry.TryGetValue(componentType, out var type)) return;

        var eid = DecodeEntityId(entityId);
        InvokeGenericMethod("RemoveComponent", type, eid);
    }

    /// <inheritdoc />
    public bool HasComponent(long entityId, string componentType)
    {
        if (!_componentTypeRegistry.TryGetValue(componentType, out var type)) return false;

        var eid = DecodeEntityId(entityId);
        var result = InvokeGenericMethod("HasComponent", type, eid);
        return result is true;
    }

    /// <inheritdoc />
    public IReadOnlyList<long> QueryEntities(IReadOnlyList<string> all, IReadOnlyList<string> any, IReadOnlyList<string> none)
    {
        var query = _world.CreateQuery();

        foreach (var name in all)
        {
            if (_componentTypeRegistry.TryGetValue(name, out var type))
            {
                var method = query.GetType().GetMethod("All");
                if (method is not null)
                {
                    method.MakeGenericMethod(type).Invoke(query, null);
                }
            }
        }

        foreach (var name in any)
        {
            if (_componentTypeRegistry.TryGetValue(name, out var type))
            {
                var method = query.GetType().GetMethod("Any");
                if (method is not null)
                {
                    method.MakeGenericMethod(type).Invoke(query, null);
                }
            }
        }

        foreach (var name in none)
        {
            if (_componentTypeRegistry.TryGetValue(name, out var type))
            {
                var method = query.GetType().GetMethod("None");
                if (method is not null)
                {
                    method.MakeGenericMethod(type).Invoke(query, null);
                }
            }
        }

        var entities = query.Build();
        return entities.Select(e => EncodeEntityId(e)).ToList();
    }

    /// <inheritdoc />
    public void Update(double deltaTime)
    {
        if (_world is Gnosis.ECS.World.World concreteWorld)
        {
            concreteWorld.Update((float)deltaTime);
        }
    }

    #region EntityId 编解码

    /// <summary>
    ///     将 EntityId 编码为 long 句柄（高 32 位 = Generation，低 32 位 = Index）
    /// </summary>
    private static long EncodeEntityId(EntityId eid)
    {
        return ((long)eid.Generation << 32) | eid.Index;
    }

    /// <summary>
    ///     从 long 句柄解码为 EntityId（保留完整的 Index + Generation）
    /// </summary>
    private static EntityId DecodeEntityId(long handle)
    {
        var index = (uint)(handle & 0xFFFFFFFF);
        var generation = (uint)((handle >> 32) & 0xFFFFFFFF);
        return new EntityId(index, generation);
    }

    #endregion

    #region 反射辅助

    /// <summary>
    ///     将字段字典应用到组件实例
    /// </summary>
    private static void ApplyFields(object component, Type type, Dictionary<string, object?> fields)
    {
        foreach (var (fieldName, value) in fields)
        {
            var prop = type.GetProperty(fieldName);
            if (prop is not null && prop.CanWrite)
            {
                prop.SetValue(component, value);
            }
        }
    }

    /// <summary>
    ///     反射调用 IWorld 的泛型方法
    /// </summary>
    private object? InvokeGenericMethod(string methodName, Type genericType, params object?[] args)
    {
        var method = _world.GetType().GetMethod(methodName);
        if (method is null) return null;

        var generic = method.MakeGenericMethod(genericType);
        return generic.Invoke(_world, args);
    }

    #endregion
}

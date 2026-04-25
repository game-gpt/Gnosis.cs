using System.Linq.Expressions;
using Gnosis.Core;
using Gnosis.ECS.Component;
using Gnosis.ECS.Query;
using Gnosis.ECS.World;

namespace Gnosis.VM;

/// <summary>
///     基于 Gnosis.ECS 的游戏世界实现
///     通过组件类型注册表将字符串类型名映射到编译委托，消除反射开销
/// </summary>
public sealed class GnosisECSWorld : IGameWorld
{
    private readonly IWorld _world;
    private readonly Dictionary<string, Type> _componentTypeRegistry;
    private readonly Dictionary<long, EntityId> _entityIdMap;

    private readonly Dictionary<string, Action<EntityId, object>> _compiledAddComponent;
    private readonly Dictionary<string, Func<EntityId, object>> _compiledGetComponent;
    private readonly Dictionary<string, Action<EntityId, object>> _compiledSetComponent;
    private readonly Dictionary<string, Action<EntityId>> _compiledRemoveComponent;
    private readonly Dictionary<string, Func<EntityId, bool>> _compiledHasComponent;
    private readonly Dictionary<string, Func<EntityId, object?, object?>> _compiledGetSetField;

    public GnosisECSWorld(IWorld world)
    {
        _world = world;
        _componentTypeRegistry = new Dictionary<string, Type>();
        _entityIdMap = new Dictionary<long, EntityId>();
        _compiledAddComponent = new Dictionary<string, Action<EntityId, object>>();
        _compiledGetComponent = new Dictionary<string, Func<EntityId, object>>();
        _compiledSetComponent = new Dictionary<string, Action<EntityId, object>>();
        _compiledRemoveComponent = new Dictionary<string, Action<EntityId>>();
        _compiledHasComponent = new Dictionary<string, Func<EntityId, bool>>();
        _compiledGetSetField = new Dictionary<string, Func<EntityId, object?, object?>>();
    }

    /// <summary>
    ///     注册组件类型，同时预编译所有泛型方法委托
    /// </summary>
    public void RegisterComponentType<T>(string name) where T : struct
    {
        _componentTypeRegistry[name] = typeof(T);

        _compiledAddComponent[name] = CompileAddComponent<T>();
        _compiledGetComponent[name] = CompileGetComponent<T>();
        _compiledSetComponent[name] = CompileSetComponent<T>();
        _compiledRemoveComponent[name] = CompileRemoveComponent<T>();
        _compiledHasComponent[name] = CompileHasComponent<T>();
    }

    /// <summary>
    ///     注册组件类型（非泛型），使用表达式树编译委托
    /// </summary>
    public void RegisterComponentType(string name, Type componentType)
    {
        if (!componentType.IsValueType)
        {
            throw new ArgumentException($"组件类型必须是值类型：{componentType.Name}");
        }

        _componentTypeRegistry[name] = componentType;

        _compiledAddComponent[name] = CompileAddComponent(componentType);
        _compiledGetComponent[name] = CompileGetComponent(componentType);
        _compiledSetComponent[name] = CompileSetComponent(componentType);
        _compiledRemoveComponent[name] = CompileRemoveComponent(componentType);
        _compiledHasComponent[name] = CompileHasComponent(componentType);
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
        if (!_compiledAddComponent.TryGetValue(componentType, out var addDelegate)) return;

        var eid = DecodeEntityId(entityId);

        if (!_componentTypeRegistry.TryGetValue(componentType, out var type)) return;

        var component = Activator.CreateInstance(type)!;
        ApplyFields(component, type, fields);
        addDelegate(eid, component);
    }

    /// <inheritdoc />
    public object? GetComponent(long entityId, string componentType, string fieldName)
    {
        if (!_compiledGetComponent.TryGetValue(componentType, out var getDelegate)) return null;

        var eid = DecodeEntityId(entityId);
        var component = getDelegate(eid);
        if (component is null) return null;

        var prop = _componentTypeRegistry[componentType].GetProperty(fieldName);
        return prop?.GetValue(component);
    }

    /// <inheritdoc />
    public void SetComponent(long entityId, string componentType, string fieldName, object? value)
    {
        if (!_compiledGetComponent.TryGetValue(componentType, out var getDelegate)) return;
        if (!_compiledSetComponent.TryGetValue(componentType, out var setDelegate)) return;

        var eid = DecodeEntityId(entityId);
        var component = getDelegate(eid);
        if (component is null) return;

        var prop = _componentTypeRegistry[componentType].GetProperty(fieldName);
        if (prop is null || !prop.CanWrite) return;

        prop.SetValue(component, value);
        setDelegate(eid, component);
    }

    /// <inheritdoc />
    public void RemoveComponent(long entityId, string componentType)
    {
        if (!_compiledRemoveComponent.TryGetValue(componentType, out var removeDelegate)) return;

        var eid = DecodeEntityId(entityId);
        removeDelegate(eid);
    }

    /// <inheritdoc />
    public bool HasComponent(long entityId, string componentType)
    {
        if (!_compiledHasComponent.TryGetValue(componentType, out var hasDelegate)) return false;

        var eid = DecodeEntityId(entityId);
        return hasDelegate(eid);
    }

    /// <inheritdoc />
    public IReadOnlyList<long> QueryEntities(IReadOnlyList<string> all, IReadOnlyList<string> any, IReadOnlyList<string> none)
    {
        var query = _world.CreateQuery();

        foreach (var name in all)
        {
            if (_componentTypeRegistry.TryGetValue(name, out var type))
            {
                if (query is EntityQuery eq)
                {
                    eq.All(type);
                }
            }
        }

        foreach (var name in any)
        {
            if (_componentTypeRegistry.TryGetValue(name, out var type))
            {
                if (query is EntityQuery eq)
                {
                    eq.Any(type);
                }
            }
        }

        foreach (var name in none)
        {
            if (_componentTypeRegistry.TryGetValue(name, out var type))
            {
                if (query is EntityQuery eq)
                {
                    eq.None(type);
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

    private static long EncodeEntityId(EntityId eid)
    {
        return ((long)eid.Generation << 32) | eid.Index;
    }

    private static EntityId DecodeEntityId(long handle)
    {
        var index = (uint)(handle & 0xFFFFFFFF);
        var generation = (uint)((handle >> 32) & 0xFFFFFFFF);
        return new EntityId(index, generation);
    }

    #endregion

    #region 字段应用

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

    #endregion

    #region 泛型委托编译

    private Action<EntityId, object> CompileAddComponent<T>() where T : struct
    {
        var worldParam = Expression.Constant(_world);
        var entityIdParam = Expression.Parameter(typeof(EntityId), "entityId");
        var componentParam = Expression.Parameter(typeof(object), "component");

        var castComponent = Expression.Convert(componentParam, typeof(T));
        var call = Expression.Call(
            worldParam,
            typeof(IWorld).GetMethod(nameof(IWorld.AddComponent))!.MakeGenericMethod(typeof(T)),
            entityIdParam,
            castComponent);

        return Expression.Lambda<Action<EntityId, object>>(call, entityIdParam, componentParam).Compile();
    }

    private Func<EntityId, object> CompileGetComponent<T>() where T : struct
    {
        var worldParam = Expression.Constant(_world);
        var entityIdParam = Expression.Parameter(typeof(EntityId), "entityId");

        var call = Expression.Call(
            worldParam,
            typeof(IWorld).GetMethod(nameof(IWorld.GetComponent))!.MakeGenericMethod(typeof(T)),
            entityIdParam);

        var boxed = Expression.Convert(call, typeof(object));
        return Expression.Lambda<Func<EntityId, object>>(boxed, entityIdParam).Compile();
    }

    private Action<EntityId, object> CompileSetComponent<T>() where T : struct
    {
        var worldParam = Expression.Constant(_world);
        var entityIdParam = Expression.Parameter(typeof(EntityId), "entityId");
        var componentParam = Expression.Parameter(typeof(object), "component");

        var castComponent = Expression.Convert(componentParam, typeof(T));
        var call = Expression.Call(
            worldParam,
            typeof(IWorld).GetMethod(nameof(IWorld.SetComponent))!.MakeGenericMethod(typeof(T)),
            entityIdParam,
            castComponent);

        return Expression.Lambda<Action<EntityId, object>>(call, entityIdParam, componentParam).Compile();
    }

    private Action<EntityId> CompileRemoveComponent<T>() where T : struct
    {
        var worldParam = Expression.Constant(_world);
        var entityIdParam = Expression.Parameter(typeof(EntityId), "entityId");

        var call = Expression.Call(
            worldParam,
            typeof(IWorld).GetMethod(nameof(IWorld.RemoveComponent))!.MakeGenericMethod(typeof(T)),
            entityIdParam);

        return Expression.Lambda<Action<EntityId>>(call, entityIdParam).Compile();
    }

    private Func<EntityId, bool> CompileHasComponent<T>() where T : struct
    {
        var worldParam = Expression.Constant(_world);
        var entityIdParam = Expression.Parameter(typeof(EntityId), "entityId");

        var call = Expression.Call(
            worldParam,
            typeof(IWorld).GetMethod(nameof(IWorld.HasComponent))!.MakeGenericMethod(typeof(T)),
            entityIdParam);

        return Expression.Lambda<Func<EntityId, bool>>(call, entityIdParam).Compile();
    }

    #endregion

    #region 非泛型委托编译

    private Action<EntityId, object> CompileAddComponent(Type componentType)
    {
        var worldParam = Expression.Constant(_world);
        var entityIdParam = Expression.Parameter(typeof(EntityId), "entityId");
        var componentParam = Expression.Parameter(typeof(object), "component");

        var castComponent = Expression.Convert(componentParam, componentType);
        var call = Expression.Call(
            worldParam,
            typeof(IWorld).GetMethod(nameof(IWorld.AddComponent))!.MakeGenericMethod(componentType),
            entityIdParam,
            castComponent);

        return Expression.Lambda<Action<EntityId, object>>(call, entityIdParam, componentParam).Compile();
    }

    private Func<EntityId, object> CompileGetComponent(Type componentType)
    {
        var worldParam = Expression.Constant(_world);
        var entityIdParam = Expression.Parameter(typeof(EntityId), "entityId");

        var call = Expression.Call(
            worldParam,
            typeof(IWorld).GetMethod(nameof(IWorld.GetComponent))!.MakeGenericMethod(componentType),
            entityIdParam);

        var boxed = Expression.Convert(call, typeof(object));
        return Expression.Lambda<Func<EntityId, object>>(boxed, entityIdParam).Compile();
    }

    private Action<EntityId, object> CompileSetComponent(Type componentType)
    {
        var worldParam = Expression.Constant(_world);
        var entityIdParam = Expression.Parameter(typeof(EntityId), "entityId");
        var componentParam = Expression.Parameter(typeof(object), "component");

        var castComponent = Expression.Convert(componentParam, componentType);
        var call = Expression.Call(
            worldParam,
            typeof(IWorld).GetMethod(nameof(IWorld.SetComponent))!.MakeGenericMethod(componentType),
            entityIdParam,
            castComponent);

        return Expression.Lambda<Action<EntityId, object>>(call, entityIdParam, componentParam).Compile();
    }

    private Action<EntityId> CompileRemoveComponent(Type componentType)
    {
        var worldParam = Expression.Constant(_world);
        var entityIdParam = Expression.Parameter(typeof(EntityId), "entityId");

        var call = Expression.Call(
            worldParam,
            typeof(IWorld).GetMethod(nameof(IWorld.RemoveComponent))!.MakeGenericMethod(componentType),
            entityIdParam);

        return Expression.Lambda<Action<EntityId>>(call, entityIdParam).Compile();
    }

    private Func<EntityId, bool> CompileHasComponent(Type componentType)
    {
        var worldParam = Expression.Constant(_world);
        var entityIdParam = Expression.Parameter(typeof(EntityId), "entityId");

        var call = Expression.Call(
            worldParam,
            typeof(IWorld).GetMethod(nameof(IWorld.HasComponent))!.MakeGenericMethod(componentType),
            entityIdParam);

        return Expression.Lambda<Func<EntityId, bool>>(call, entityIdParam).Compile();
    }

    #endregion
}

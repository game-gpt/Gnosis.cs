using Gnosis.ECS.Component;
using Gnosis.ECS.Entity;
using Gnosis.ECS.World;

namespace Gnosis.Runtime.VM;

/// <summary>
/// 组件类型注册表，桥接字节码中的类型索引与运行时 Type。
/// VM 通过此注册表将字节码的 typeIdx 解析为实际的组件类型，
/// 再通过 ComponentManager 的非泛型 API 执行组件操作。
/// </summary>
public sealed class ComponentTypeRegistry
{
    private readonly ComponentTypeId _typeIdRegistry;
    private readonly Dictionary<int, Type> _localIndexToType;
    private readonly Dictionary<Type, int> _typeToLocalIndex;
    private readonly Dictionary<string, int> _nameToIndex;

    /// <summary>
    /// 关联的组件类型 ID 注册表
    /// </summary>
    public ComponentTypeId TypeIdRegistry => _typeIdRegistry;

    public ComponentTypeRegistry()
    {
        _typeIdRegistry = new ComponentTypeId();
        _localIndexToType = new Dictionary<int, Type>();
        _typeToLocalIndex = new Dictionary<Type, int>();
        _nameToIndex = new Dictionary<string, int>();
    }

    public ComponentTypeRegistry(ComponentTypeId typeIdRegistry)
    {
        _typeIdRegistry = typeIdRegistry;
        _localIndexToType = new Dictionary<int, Type>();
        _typeToLocalIndex = new Dictionary<Type, int>();
        _nameToIndex = new Dictionary<string, int>();
    }

    /// <summary>
    /// 注册组件类型，返回分配的字节码类型索引
    /// </summary>
    public int Register(Type componentType)
    {
        if (_typeToLocalIndex.TryGetValue(componentType, out var existingIdx))
        {
            return existingIdx;
        }

        var id = _typeIdRegistry.GetOrRegister(componentType);
        _localIndexToType[id] = componentType;
        _typeToLocalIndex[componentType] = id;
        return id;
    }

    /// <summary>
    /// 注册组件类型（泛型版本）
    /// </summary>
    public int Register<T>() where T : struct
    {
        return Register(typeof(T));
    }

    /// <summary>
    /// 根据组件名称注册，用于 DefineComponent 指令中按名称注册动态组件类型
    /// </summary>
    public int RegisterByName(string name, Type componentType)
    {
        var id = Register(componentType);
        _nameToIndex[name] = id;
        return id;
    }

    /// <summary>
    /// 根据字节码类型索引获取运行时 Type
    /// </summary>
    public Type? GetType(int typeIdx)
    {
        return _localIndexToType.GetValueOrDefault(typeIdx);
    }

    /// <summary>
    /// 根据字节码类型索引获取运行时 CLR Type（GetComponent 等指令使用）
    /// </summary>
    public Type? GetClrType(int typeIdx)
    {
        return _localIndexToType.GetValueOrDefault(typeIdx);
    }

    /// <summary>
    /// 根据运行时 Type 获取字节码类型索引
    /// </summary>
    public int GetIndex(Type componentType)
    {
        return _typeToLocalIndex.GetValueOrDefault(componentType, -1);
    }

    /// <summary>
    /// 根据组件名称获取字节码类型索引
    /// </summary>
    public int GetIndex(string name)
    {
        return _nameToIndex.GetValueOrDefault(name, -1);
    }

    /// <summary>
    /// 根据字节码类型索引获取运行时 Type，未找到则抛出异常
    /// </summary>
    public Type GetRequiredType(int typeIdx)
    {
        if (_localIndexToType.TryGetValue(typeIdx, out var type))
        {
            return type;
        }

        throw new VMRuntimeException($"组件类型索引 {typeIdx} 未注册");
    }

    /// <summary>
    /// 已注册的组件类型数量
    /// </summary>
    public int Count => _localIndexToType.Count;

    /// <summary>
    /// 检查指定类型索引是否已注册
    /// </summary>
    public bool IsRegistered(int typeIdx)
    {
        return _localIndexToType.ContainsKey(typeIdx);
    }

    #region 组件操作（通过 IWorld 非泛型 API）

    /// <summary>
    /// 添加组件到指定实体，使用非泛型 ComponentPool API
    /// </summary>
    public void AddComponent(IWorld world, EntityId entity, int typeIdx)
    {
        var clrType = GetClrType(typeIdx);
        if (clrType is null)
        {
            return;
        }

        var pool = GetOrCreatePool(world, clrType);
        var componentData = Activator.CreateInstance(clrType);
        if (componentData is not null)
        {
            pool.AddComponentData(entity, componentData);
        }
    }

    /// <summary>
    /// 获取指定实体的组件数据，封装为 GGValue
    /// </summary>
    public GGValue GetComponent(IWorld world, EntityId entity, int typeIdx)
    {
        var clrType = GetClrType(typeIdx);
        if (clrType is null)
        {
            return GGValue.Null;
        }

        if (world is World concreteWorld)
        {
            var pool = concreteWorld.Components.GetPool(clrType);
            if (pool is null)
            {
                return GGValue.Null;
            }

            var data = pool.GetComponentData(entity);
            if (data is null)
            {
                return GGValue.Null;
            }

            return GGValue.FromNativeObject(data);
        }

        return GGValue.Null;
    }

    /// <summary>
    /// 设置指定实体的组件数据
    /// </summary>
    public void SetComponent(IWorld world, EntityId entity, int typeIdx, GGStruct data)
    {
        var clrType = GetClrType(typeIdx);
        if (clrType is null)
        {
            return;
        }

        var pool = GetOrCreatePool(world, clrType);
        var componentData = Activator.CreateInstance(clrType);
        if (componentData is not null)
        {
            pool.AddComponentData(entity, componentData);
        }
    }

    /// <summary>
    /// 移除指定实体的组件
    /// </summary>
    public void RemoveComponent(IWorld world, EntityId entity, int typeIdx)
    {
        var clrType = GetClrType(typeIdx);
        if (clrType is null)
        {
            return;
        }

        if (world is World concreteWorld)
        {
            var pool = concreteWorld.Components.GetPool(clrType);
            pool?.RemoveEntity(entity);
        }
    }

    /// <summary>
    /// 检查指定实体是否拥有此类型组件
    /// </summary>
    public bool HasComponent(IWorld world, EntityId entity, int typeIdx)
    {
        var clrType = GetClrType(typeIdx);
        if (clrType is null)
        {
            return false;
        }

        if (world is World concreteWorld)
        {
            var pool = concreteWorld.Components.GetPool(clrType);
            return pool?.HasEntity(entity) ?? false;
        }

        return false;
    }

    private IComponentPool GetOrCreatePool(IWorld world, Type clrType)
    {
        if (world is World concreteWorld)
        {
            return concreteWorld.Components.GetOrCreatePool(clrType);
        }

        throw new VMRuntimeException("World 不是具体实现类型，无法访问组件管理器");
    }

    #endregion
}

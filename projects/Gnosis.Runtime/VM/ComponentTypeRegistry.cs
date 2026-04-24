using Gnosis.Core;
using Gnosis.ECS.Component;
using Gnosis.ECS.Entity;
using Gnosis.ECS.World;

namespace Gnosis.Runtime.VM;

/// <summary>
/// 组件类型注册表，桥接字节码中的类型索引与运行时组件存储。
/// 支持两种组件存储模式：
///   1. C# 原生组件：通过 ComponentTypeId 注册，使用 ComponentPool&lt;T&gt; 存储
///   2. 脚本组件：通过 ScriptComponentStorage 存储，使用 GGStruct 表示数据
/// </summary>
public sealed class ComponentTypeRegistry
{
    #region 字段

    private readonly ComponentTypeId _typeIdRegistry;
    private readonly Dictionary<int, Type> _localIndexToType;
    private readonly Dictionary<Type, int> _typeToLocalIndex;
    private readonly Dictionary<string, int> _nameToIndex;
    private readonly ScriptComponentStorage _scriptStorage;
    private readonly HashSet<int> _scriptTypeIndices;

    #endregion

    #region 属性

    /// <summary>
    /// 关联的组件类型 ID 注册表
    /// </summary>
    public ComponentTypeId TypeIdRegistry => _typeIdRegistry;

    /// <summary>
    /// 脚本组件存储
    /// </summary>
    public ScriptComponentStorage ScriptStorage => _scriptStorage;

    #endregion

    #region 构造函数

    public ComponentTypeRegistry()
    {
        _typeIdRegistry = new ComponentTypeId();
        _localIndexToType = new Dictionary<int, Type>();
        _typeToLocalIndex = new Dictionary<Type, int>();
        _nameToIndex = new Dictionary<string, int>();
        _scriptStorage = new ScriptComponentStorage();
        _scriptTypeIndices = new HashSet<int>();
    }

    public ComponentTypeRegistry(ComponentTypeId typeIdRegistry)
    {
        _typeIdRegistry = typeIdRegistry;
        _localIndexToType = new Dictionary<int, Type>();
        _typeToLocalIndex = new Dictionary<Type, int>();
        _nameToIndex = new Dictionary<string, int>();
        _scriptStorage = new ScriptComponentStorage();
        _scriptTypeIndices = new HashSet<int>();
    }

    #endregion

    #region C# 原生组件注册

    /// <summary>
    /// 注册 C# 原生组件类型，返回分配的字节码类型索引
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
    /// 注册 C# 原生组件类型（泛型版本）
    /// </summary>
    public int Register<T>() where T : struct
    {
        return Register(typeof(T));
    }

    /// <summary>
    /// 根据组件名称注册 C# 原生组件类型
    /// </summary>
    public int RegisterByName(string name, Type componentType)
    {
        var id = Register(componentType);
        _nameToIndex[name] = id;
        return id;
    }

    #endregion

    #region 脚本组件注册

    /// <summary>
    /// 注册脚本组件类型，返回分配的字节码类型索引。
    /// 脚本组件使用 ScriptComponentStorage 存储，不依赖 C# struct 类型。
    /// </summary>
    public int RegisterScriptComponent(ScriptComponentType type)
    {
        var idx = _scriptStorage.RegisterType(type);
        _scriptTypeIndices.Add(idx);
        _nameToIndex[type.Name] = idx;
        return idx;
    }

    /// <summary>
    /// 检查指定类型索引是否为脚本组件
    /// </summary>
    public bool IsScriptComponent(int typeIdx)
    {
        return _scriptTypeIndices.Contains(typeIdx);
    }

    #endregion

    #region 类型查询

    /// <summary>
    /// 根据字节码类型索引获取运行时 CLR Type（仅 C# 原生组件）
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
    /// 已注册的组件类型数量（C# 原生 + 脚本）
    /// </summary>
    public int Count => _localIndexToType.Count + _scriptStorage.TypeCount;

    /// <summary>
    /// 检查指定类型索引是否已注册
    /// </summary>
    public bool IsRegistered(int typeIdx)
    {
        return _localIndexToType.ContainsKey(typeIdx) || _scriptTypeIndices.Contains(typeIdx);
    }

    #endregion

    #region 组件操作（统一入口，自动分派到 C# 原生或脚本存储）

    /// <summary>
    /// 添加组件到指定实体。
    /// 脚本组件使用默认值初始化，C# 原生组件使用 Activator.CreateInstance 创建。
    /// </summary>
    public void AddComponent(IWorld world, EntityId entity, int typeIdx)
    {
        if (IsScriptComponent(typeIdx))
        {
            _scriptStorage.AddComponent(entity, typeIdx);
            return;
        }

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
    /// 添加脚本组件到指定实体，使用指定数据初始化
    /// </summary>
    public void AddScriptComponent(EntityId entity, int typeIdx, GGStruct componentData)
    {
        if (IsScriptComponent(typeIdx))
        {
            _scriptStorage.AddComponent(entity, typeIdx, componentData);
        }
    }

    /// <summary>
    /// 获取指定实体的组件数据，封装为 GGValue。
    /// 脚本组件返回 GGStruct 引用，C# 原生组件返回 NativeObject 引用。
    /// </summary>
    public GGValue GetComponent(IWorld world, EntityId entity, int typeIdx)
    {
        if (IsScriptComponent(typeIdx))
        {
            var data = _scriptStorage.GetComponent(entity, typeIdx);
            return data is not null ? GGValue.FromStruct(data) : GGValue.Null;
        }

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
    /// 设置指定实体的组件数据。
    /// 脚本组件直接存储 GGStruct，C# 原生组件使用默认实例替换。
    /// </summary>
    public void SetComponent(IWorld world, EntityId entity, int typeIdx, GGStruct data)
    {
        if (IsScriptComponent(typeIdx))
        {
            _scriptStorage.SetComponent(entity, typeIdx, data);
            return;
        }

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
        if (IsScriptComponent(typeIdx))
        {
            _scriptStorage.RemoveComponent(entity, typeIdx);
            return;
        }

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
        if (IsScriptComponent(typeIdx))
        {
            return _scriptStorage.HasComponent(entity, typeIdx);
        }

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

    /// <summary>
    /// 清除指定实体的所有脚本组件
    /// </summary>
    public void RemoveAllScriptComponents(EntityId entity)
    {
        _scriptStorage.RemoveAllComponents(entity);
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

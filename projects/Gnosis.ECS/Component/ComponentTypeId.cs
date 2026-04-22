namespace Gnosis.ECS.Component;

/// <summary>
/// 组件类型标识符，为每种组件类型分配紧凑的整型 ID。
/// 线程安全的类型注册表。
/// </summary>
public sealed class ComponentTypeId
{
    private readonly Dictionary<Type, int> _typeToId;
    private readonly Dictionary<int, Type> _idToType;
    private int _nextId;

    /// <summary>
    /// 已注册的组件类型数量
    /// </summary>
    public int Count => _typeToId.Count;

    public ComponentTypeId()
    {
        _typeToId = new Dictionary<Type, int>();
        _idToType = new Dictionary<int, Type>();
        _nextId = 0;
    }

    /// <summary>
    /// 获取或注册指定组件类型的 ID
    /// </summary>
    public int GetOrRegister<T>() where T : struct
    {
        return GetOrRegister(typeof(T));
    }

    /// <summary>
    /// 获取或注册指定组件类型的 ID
    /// </summary>
    public int GetOrRegister(Type type)
    {
        if (_typeToId.TryGetValue(type, out var id))
        {
            return id;
        }

        lock (_typeToId)
        {
            if (_typeToId.TryGetValue(type, out id))
            {
                return id;
            }

            id = _nextId++;
            _typeToId[type] = id;
            _idToType[id] = type;

            return id;
        }
    }

    /// <summary>
    /// 根据类型获取已注册的 ID，未注册返回 -1
    /// </summary>
    public int GetId<T>() where T : struct
    {
        return _typeToId.GetValueOrDefault(typeof(T), -1);
    }

    /// <summary>
    /// 根据 ID 获取组件类型
    /// </summary>
    public Type GetType(int id)
    {
        return _idToType.TryGetValue(id, out var type) ? type : throw new KeyNotFoundException($"组件类型 ID {id} 未注册");
    }

    /// <summary>
    /// 检查指定组件类型是否已注册
    /// </summary>
    public bool IsRegistered<T>() where T : struct
    {
        return _typeToId.ContainsKey(typeof(T));
    }
}

using Gnosis.Core;

namespace Gnosis.Runtime.VM;

/// <summary>
/// 脚本组件类型描述，由 DefineComponent 指令创建。
/// 记录组件名称和字段布局，用于运行时创建和验证组件数据。
/// </summary>
public sealed class ScriptComponentType
{
    public string Name { get; }
    public string[] FieldNames { get; }
    public int FieldCount => FieldNames.Length;

    public ScriptComponentType(string name, string[] fieldNames)
    {
        Name = name;
        FieldNames = fieldNames;
    }
}

/// <summary>
/// 脚本组件存储，为 GGScript 定义的组件提供独立的存储层。
/// 与 C# struct 组件的 ComponentPool 不同，脚本组件使用 GGStruct 存储数据，
/// 不需要预定义 C# 类型，支持运行时动态创建。
/// </summary>
public sealed class ScriptComponentStorage
{
    #region 字段

    private readonly Dictionary<int, Dictionary<EntityId, GGStruct>> _data = new();
    private readonly Dictionary<int, ScriptComponentType> _types = new();
    private readonly Dictionary<string, int> _nameToIndex = new();
    private int _nextTypeIndex;

    #endregion

    #region 组件类型注册

    /// <summary>
    /// 注册脚本组件类型，返回分配的类型索引
    /// </summary>
    public int RegisterType(ScriptComponentType type)
    {
        if (_nameToIndex.TryGetValue(type.Name, out var existingIdx))
        {
            return existingIdx;
        }

        var idx = _nextTypeIndex++;
        _types[idx] = type;
        _nameToIndex[type.Name] = idx;
        _data[idx] = new Dictionary<EntityId, GGStruct>();
        return idx;
    }

    /// <summary>
    /// 根据名称获取类型索引，未找到返回 -1
    /// </summary>
    public int GetTypeIndex(string name)
    {
        return _nameToIndex.GetValueOrDefault(name, -1);
    }

    /// <summary>
    /// 根据索引获取组件类型描述
    /// </summary>
    public ScriptComponentType? GetType(int typeIdx)
    {
        return _types.GetValueOrDefault(typeIdx);
    }

    /// <summary>
    /// 已注册的脚本组件类型数量
    /// </summary>
    public int TypeCount => _types.Count;

    /// <summary>
    /// 检查类型索引是否已注册
    /// </summary>
    public bool IsTypeRegistered(int typeIdx)
    {
        return _types.ContainsKey(typeIdx);
    }

    #endregion

    #region 组件数据操作

    /// <summary>
    /// 为实体添加脚本组件，使用默认值初始化
    /// </summary>
    public void AddComponent(EntityId entity, int typeIdx)
    {
        if (!_types.TryGetValue(typeIdx, out var type))
        {
            return;
        }

        var data = _data[typeIdx];
        if (!data.ContainsKey(entity))
        {
            var ggStruct = new GGStruct(type.Name, type.FieldNames);
            data[entity] = ggStruct;
        }
    }

    /// <summary>
    /// 为实体添加脚本组件，使用指定数据初始化
    /// </summary>
    public void AddComponent(EntityId entity, int typeIdx, GGStruct componentData)
    {
        if (!_types.ContainsKey(typeIdx))
        {
            return;
        }

        _data[typeIdx][entity] = componentData;
    }

    /// <summary>
    /// 获取实体的脚本组件数据，未找到返回 null
    /// </summary>
    public GGStruct? GetComponent(EntityId entity, int typeIdx)
    {
        if (!_data.TryGetValue(typeIdx, out var entities))
        {
            return null;
        }

        return entities.GetValueOrDefault(entity);
    }

    /// <summary>
    /// 设置实体的脚本组件数据
    /// </summary>
    public void SetComponent(EntityId entity, int typeIdx, GGStruct componentData)
    {
        if (!_types.ContainsKey(typeIdx))
        {
            return;
        }

        if (!_data.TryGetValue(typeIdx, out var entities))
        {
            entities = new Dictionary<EntityId, GGStruct>();
            _data[typeIdx] = entities;
        }

        entities[entity] = componentData;
    }

    /// <summary>
    /// 移除实体的脚本组件
    /// </summary>
    public void RemoveComponent(EntityId entity, int typeIdx)
    {
        if (_data.TryGetValue(typeIdx, out var entities))
        {
            entities.Remove(entity);
        }
    }

    /// <summary>
    /// 检查实体是否拥有指定脚本组件
    /// </summary>
    public bool HasComponent(EntityId entity, int typeIdx)
    {
        if (!_data.TryGetValue(typeIdx, out var entities))
        {
            return false;
        }

        return entities.ContainsKey(entity);
    }

    /// <summary>
    /// 获取拥有指定脚本组件的所有实体 ID
    /// </summary>
    public IReadOnlyList<EntityId> GetEntitiesWithComponent(int typeIdx)
    {
        if (!_data.TryGetValue(typeIdx, out var entities))
        {
            return Array.Empty<EntityId>();
        }

        return entities.Keys.ToList();
    }

    /// <summary>
    /// 获取拥有所有指定脚本组件类型的实体
    /// </summary>
    public List<EntityId> QueryAll(int[] typeIndices)
    {
        if (typeIndices.Length == 0)
        {
            return new List<EntityId>();
        }

        var firstIdx = typeIndices[0];
        if (!_data.TryGetValue(firstIdx, out var firstEntities))
        {
            return new List<EntityId>();
        }

        var candidates = new List<EntityId>(firstEntities.Keys);

        for (var i = 1; i < typeIndices.Length; i++)
        {
            if (!_data.TryGetValue(typeIndices[i], out var entities))
            {
                return new List<EntityId>();
            }

            candidates.RemoveAll(id => !entities.ContainsKey(id));
        }

        return candidates;
    }

    /// <summary>
    /// 获取拥有任一指定脚本组件类型的实体
    /// </summary>
    public List<EntityId> QueryAny(int[] typeIndices)
    {
        var result = new HashSet<EntityId>();

        foreach (var typeIdx in typeIndices)
        {
            if (_data.TryGetValue(typeIdx, out var entities))
            {
                foreach (var id in entities.Keys)
                {
                    result.Add(id);
                }
            }
        }

        return result.ToList();
    }

    /// <summary>
    /// 获取拥有指定脚本组件的实体（单类型查询）
    /// </summary>
    public List<EntityId> QueryWith(int typeIdx)
    {
        return QueryAll(new[] { typeIdx });
    }

    /// <summary>
    /// 获取不拥有指定脚本组件的实体（需要传入所有实体列表）
    /// </summary>
    public List<EntityId> QueryWithout(int typeIdx, IEnumerable<EntityId> allEntities)
    {
        var excluded = _data.TryGetValue(typeIdx, out var entities)
            ? (IEnumerable<EntityId>)entities.Keys
            : Array.Empty<EntityId>();

        var excludedSet = new HashSet<EntityId>(excluded);
        return allEntities.Where(id => !excludedSet.Contains(id)).ToList();
    }

    /// <summary>
    /// 清除指定实体的所有脚本组件
    /// </summary>
    public void RemoveAllComponents(EntityId entity)
    {
        foreach (var entities in _data.Values)
        {
            entities.Remove(entity);
        }
    }

    #endregion
}

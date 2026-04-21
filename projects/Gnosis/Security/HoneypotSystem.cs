using Gnosis.Core;

namespace Gnosis.Security;

/// <summary>
/// 蜜罐系统，管理蜜罐字段和蜜罐实体，检测作弊行为
/// </summary>
public sealed class HoneypotSystem
{
    #region 字段

    private readonly Dictionary<string, HoneypotField> _fields = new();
    private readonly List<HoneypotEntity> _entities = [];

    #endregion

    #region 属性

    /// <summary>
    /// 获取已注册的蜜罐字段数量
    /// </summary>
    public int FieldCount => _fields.Count;

    /// <summary>
    /// 获取已注册的蜜罐实体数量
    /// </summary>
    public int EntityCount => _entities.Count;

    #endregion

    #region 公开方法

    /// <summary>
    /// 注册蜜罐字段，使用诱惑性名称和假值
    /// </summary>
    /// <param name="name">蜜罐字段名称</param>
    /// <param name="fakeValue">诱饵假值</param>
    public void RegisterField(string name, object fakeValue)
    {
        _fields[name] = new HoneypotField(name, fakeValue);
    }

    /// <summary>
    /// 注册蜜罐实体，使用不可达位置
    /// </summary>
    /// <param name="entityId">蜜罐实体 ID</param>
    /// <param name="fakePosition">不可达位置</param>
    public void RegisterEntity(EntityId entityId, float x = 0f, float y = -9999f, float z = 0f)
    {
        _entities.Add(new HoneypotEntity(entityId, x, y, z));
    }

    /// <summary>
    /// 检查指定蜜罐字段是否被触发
    /// </summary>
    /// <param name="name">蜜罐字段名称</param>
    /// <returns>如果字段被触发则返回 true</returns>
    public bool IsFieldTriggered(string name)
    {
        return _fields.TryGetValue(name, out var field) && field.IsTriggered;
    }

    /// <summary>
    /// 检查指定蜜罐实体是否被触发
    /// </summary>
    /// <param name="entityId">蜜罐实体 ID</param>
    /// <returns>如果实体被触发则返回 true</returns>
    public bool IsEntityTriggered(EntityId entityId)
    {
        return _entities.Exists(e => e.EntityId == entityId && e.IsTriggered);
    }

    /// <summary>
    /// 获取所有被触发的蜜罐字段名称
    /// </summary>
    /// <returns>被触发的蜜罐字段名称列表</returns>
    public List<string> GetTriggeredFields()
    {
        var result = new List<string>();

        foreach (var kvp in _fields)
        {
            if (kvp.Value.IsTriggered)
            {
                result.Add(kvp.Key);
            }
        }

        return result;
    }

    /// <summary>
    /// 标记蜜罐字段为已触发
    /// </summary>
    /// <param name="name">蜜罐字段名称</param>
    public void MarkFieldTriggered(string name)
    {
        if (_fields.TryGetValue(name, out var field))
        {
            field.MarkTriggered();
        }
    }

    /// <summary>
    /// 标记蜜罐实体为已触发
    /// </summary>
    /// <param name="entityId">蜜罐实体 ID</param>
    public void MarkEntityTriggered(EntityId entityId)
    {
        var entity = _entities.Find(e => e.EntityId == entityId);

        if (entity is not null)
        {
            entity.MarkTriggered();
        }
    }

    /// <summary>
    /// 清除所有蜜罐的触发状态
    /// </summary>
    public void ResetAll()
    {
        foreach (var field in _fields.Values)
        {
            field.Reset();
        }

        foreach (var entity in _entities)
        {
            entity.Reset();
        }
    }

    #endregion

    #region 内部类

    private sealed class HoneypotField : IHoneypot
    {
        public string Name { get; }
        public object FakeValue { get; }
        public bool IsTriggered { get; private set; }

        public event Action? OnTriggered;

        public HoneypotField(string name, object fakeValue)
        {
            Name = name;
            FakeValue = fakeValue;
        }

        public void MarkTriggered()
        {
            IsTriggered = true;
            OnTriggered?.Invoke();
        }

        public void Reset()
        {
            IsTriggered = false;
        }
    }

    private sealed class HoneypotEntity
    {
        public EntityId EntityId { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public bool IsTriggered { get; private set; }

        public HoneypotEntity(EntityId entityId, float x, float y, float z)
        {
            EntityId = entityId;
            X = x;
            Y = y;
            Z = z;
        }

        public void MarkTriggered()
        {
            IsTriggered = true;
        }

        public void Reset()
        {
            IsTriggered = false;
        }
    }

    #endregion
}

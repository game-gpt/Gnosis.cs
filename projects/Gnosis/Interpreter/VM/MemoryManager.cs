namespace Gnosis.Interpreter.VM;

/// <summary>
/// 虚拟机对象
/// </summary>
internal class VMObject
{
    #region Fields

    /// <summary>
    /// 对象值
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// 引用计数
    /// </summary>
    public int ReferenceCount { get; set; }

    /// <summary>
    /// 该对象引用的其他对象
    /// </summary>
    public List<VMObject> References { get; } = [];

    /// <summary>
    /// 用于标记-清除
    /// </summary>
    public bool IsMarked { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// 初始化虚拟机对象
    /// </summary>
    public VMObject(object value)
    {
        Value = value;
        ReferenceCount = 1;
    }

    #endregion
}

/// <summary>
/// 基于引用计数的内存管理器
/// </summary>
public class MemoryManager
{
    #region Fields

    private readonly Dictionary<int, VMObject> _objects = new();
    private int _nextId;
    private const int CycleCheckInterval = 256;
    private int _allocationsSinceLastCheck;

    #endregion

    #region Constructors
    #endregion

    #region Public Methods

    /// <summary>
    /// 分配新对象，引用计数初始化为 1，返回对象 ID
    /// </summary>
    public int Allocate(object value)
    {
        var id = _nextId++;
        var obj = new VMObject(value);
        _objects[id] = obj;
        return id;
    }

    /// <summary>
    /// 增加引用计数
    /// </summary>
    public void Retain(int objectId)
    {
        if (!_objects.TryGetValue(objectId, out var obj))
        {
            throw new KeyNotFoundException($"对象 ID {objectId} 不存在");
        }

        obj.ReferenceCount++;
    }

    /// <summary>
    /// 减少引用计数，引用计数归零时从 _objects 移除
    /// </summary>
    public void Release(int objectId)
    {
        if (!_objects.TryGetValue(objectId, out var obj))
        {
            return;
        }

        obj.ReferenceCount--;

        if (obj.ReferenceCount <= 0)
        {
            _objects.Remove(objectId);
        }

        _allocationsSinceLastCheck++;

        if (_allocationsSinceLastCheck >= CycleCheckInterval)
        {
            CollectCycles();
        }
    }

    /// <summary>
    /// 获取对象值，不存在返回 null
    /// </summary>
    public object? GetValue(int objectId)
    {
        if (!_objects.TryGetValue(objectId, out var obj))
        {
            return null;
        }

        return obj.Value;
    }

    /// <summary>
    /// 设置对象引用关系（from 引用 to）
    /// </summary>
    public void SetReference(int fromId, int toId)
    {
        if (!_objects.TryGetValue(fromId, out var fromObj))
        {
            return;
        }

        if (!_objects.TryGetValue(toId, out var toObj))
        {
            return;
        }

        fromObj.References.Add(toObj);
        toObj.ReferenceCount++;
    }

    /// <summary>
    /// 移除对象引用关系
    /// </summary>
    public void RemoveReference(int fromId, int toId)
    {
        if (!_objects.TryGetValue(fromId, out var fromObj))
        {
            return;
        }

        if (!_objects.TryGetValue(toId, out var toObj))
        {
            return;
        }

        if (fromObj.References.Remove(toObj))
        {
            toObj.ReferenceCount--;
        }
    }

    /// <summary>
    /// 标记-清除辅助回收
    /// </summary>
    public void CollectCycles()
    {
        foreach (var obj in _objects.Values)
        {
            obj.IsMarked = false;
        }

        foreach (var obj in _objects.Values)
        {
            if (obj.ReferenceCount > 0)
            {
                MarkReachable(obj);
            }
        }

        var unreachableIds = _objects
            .Where(kvp => kvp.Value is { IsMarked: false, ReferenceCount: <= 0 })
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in unreachableIds)
        {
            _objects.Remove(id);
        }

        _allocationsSinceLastCheck = 0;
    }

    /// <summary>
    /// 清空所有对象
    /// </summary>
    public void Clear()
    {
        _objects.Clear();
        _nextId = 0;
        _allocationsSinceLastCheck = 0;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 递归标记可达对象
    /// </summary>
    private void MarkReachable(VMObject obj)
    {
        if (obj.IsMarked)
        {
            return;
        }

        obj.IsMarked = true;

        foreach (var reference in obj.References)
        {
            MarkReachable(reference);
        }
    }

    #endregion
}

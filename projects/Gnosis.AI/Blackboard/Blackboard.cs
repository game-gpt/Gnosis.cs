namespace Gnosis.AI.Blackboard;

/// <summary>
/// 黑板实现类，提供 AI 模块间的数据共享
/// </summary>
public sealed class Blackboard : IBlackboard
{
    #region 字段

    private readonly Dictionary<string, object> _values = new();

    #endregion

    #region IBlackboard 实现

    /// <summary>
    /// 设置键值
    /// </summary>
    public void SetValue<T>(string key, T value)
    {
        _values[key] = value!;
    }

    /// <summary>
    /// 获取键值
    /// </summary>
    public T? GetValue<T>(string key)
    {
        if (_values.TryGetValue(key, out var value) && value is T typed)
        {
            return typed;
        }

        return default;
    }

    /// <summary>
    /// 是否包含指定键
    /// </summary>
    public bool HasKey(string key)
    {
        return _values.ContainsKey(key);
    }

    /// <summary>
    /// 移除指定键
    /// </summary>
    public void RemoveKey(string key)
    {
        _values.Remove(key);
    }

    /// <summary>
    /// 清空所有键值
    /// </summary>
    public void Clear()
    {
        _values.Clear();
    }

    #endregion
}

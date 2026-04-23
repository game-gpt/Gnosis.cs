namespace Gnosis.AI.Planning;

/// <summary>
/// 世界状态实现，基于布尔键值对表示 GOAP 的状态空间
/// </summary>
public sealed class WorldState : IWorldState
{
    #region 字段

    private readonly Dictionary<string, bool> _state;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建空的世界状态
    /// </summary>
    public WorldState()
    {
        _state = new Dictionary<string, bool>();
    }

    /// <summary>
    /// 从已有状态创建世界状态
    /// </summary>
    /// <param name="state">初始状态</param>
    public WorldState(Dictionary<string, bool> state)
    {
        _state = new Dictionary<string, bool>(state);
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 获取状态值
    /// </summary>
    /// <param name="key">状态键</param>
    /// <returns>状态值，不存在则返回 false</returns>
    public bool Get(string key)
    {
        return _state.GetValueOrDefault(key, false);
    }

    /// <summary>
    /// 设置状态值
    /// </summary>
    /// <param name="key">状态键</param>
    /// <param name="value">状态值</param>
    public void Set(string key, bool value)
    {
        _state[key] = value;
    }

    /// <summary>
    /// 是否包含指定状态键
    /// </summary>
    /// <param name="key">状态键</param>
    /// <returns>是否包含</returns>
    public bool Has(string key)
    {
        return _state.ContainsKey(key);
    }

    /// <summary>
    /// 获取所有状态键
    /// </summary>
    /// <returns>状态键列表</returns>
    public IReadOnlyList<string> GetKeys()
    {
        return _state.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// 与另一个世界状态的差异
    /// </summary>
    /// <param name="other">另一个世界状态</param>
    /// <returns>差异键列表</returns>
    public IReadOnlyList<string> Diff(IWorldState other)
    {
        var diff = new List<string>();

        foreach (var key in GetKeys())
        {
            if (!other.Has(key) || other.Get(key) != Get(key))
            {
                diff.Add(key);
            }
        }

        foreach (var key in other.GetKeys())
        {
            if (!Has(key))
            {
                diff.Add(key);
            }
        }

        return diff.AsReadOnly();
    }

    /// <summary>
    /// 满足另一个世界状态的所有条件
    /// </summary>
    /// <param name="other">目标世界状态</param>
    /// <returns>是否满足</returns>
    public bool Satisfies(IWorldState other)
    {
        foreach (var key in other.GetKeys())
        {
            if (!Has(key) || Get(key) != other.Get(key))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 复制当前世界状态
    /// </summary>
    /// <returns>克隆的世界状态</returns>
    public IWorldState Clone()
    {
        return new WorldState(_state);
    }

    #endregion
}

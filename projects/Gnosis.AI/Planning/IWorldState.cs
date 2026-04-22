namespace Gnosis.AI.Planning;

/// <summary>
/// 世界状态接口，用于 GOAP 规划器的状态表示
/// </summary>
public interface IWorldState
{
    /// <summary>
    /// 获取状态值
    /// </summary>
    bool Get(string key);

    /// <summary>
    /// 设置状态值
    /// </summary>
    void Set(string key, bool value);

    /// <summary>
    /// 是否包含指定状态键
    /// </summary>
    bool Has(string key);

    /// <summary>
    /// 与另一个世界状态的差异
    /// </summary>
    IReadOnlyList<string> Diff(IWorldState other);

    /// <summary>
    /// 满足另一个世界状态的所有条件
    /// </summary>
    bool Satisfies(IWorldState other);

    /// <summary>
    /// 复制当前世界状态
    /// </summary>
    IWorldState Clone();
}

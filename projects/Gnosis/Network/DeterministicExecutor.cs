namespace Gnosis.Network;

/// <summary>
/// 确定性逻辑执行器，确保所有客户端在相同输入下产生相同结果
/// </summary>
public sealed class DeterministicExecutor
{
    #region 属性

    /// <summary>
    /// 随机数种子
    /// </summary>
    public uint RandomSeed { get; private set; }

    /// <summary>
    /// 当前逻辑帧号
    /// </summary>
    public int CurrentFrame { get; private set; }

    /// <summary>
    /// 固定时间步长（秒）
    /// </summary>
    public float FixedDeltaTime { get; set; }

    /// <summary>
    /// 是否使用定点数运算
    /// </summary>
    public bool UseFixedPoint { get; set; } = false;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化确定性执行器
    /// </summary>
    /// <param name="tickRate">帧率</param>
    public DeterministicExecutor(int tickRate = 30)
    {
        FixedDeltaTime = 1.0f / tickRate;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 使用指定种子初始化随机数生成器
    /// </summary>
    /// <param name="seed">随机种子</param>
    public void InitializeRandom(uint seed)
    {
        RandomSeed = seed;

        throw new NotImplementedException("确定性执行器尚未实现");
    }

    /// <summary>
    /// 执行一帧确定性逻辑
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <param name="inputs">所有玩家输入</param>
    public void ExecuteFrame(int frame, System.Collections.Generic.IReadOnlyDictionary<int, byte[]> inputs)
    {
        throw new NotImplementedException("确定性执行器尚未实现");
    }

    /// <summary>
    /// 生成确定性随机数
    /// </summary>
    /// <param name="maxValue">最大值（不含）</param>
    /// <returns>随机数</returns>
    public int NextRandom(int maxValue)
    {
        throw new NotImplementedException("确定性执行器尚未实现");
    }

    /// <summary>
    /// 计算当前状态哈希
    /// </summary>
    /// <returns>状态哈希值</returns>
    public uint ComputeStateHash()
    {
        throw new NotImplementedException("确定性执行器尚未实现");
    }

    #endregion
}

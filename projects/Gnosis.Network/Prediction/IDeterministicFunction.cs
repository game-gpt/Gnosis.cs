namespace Gnosis.Network.Prediction;

/// <summary>
/// 确定性函数接口，用于帧同步逻辑的确定性执行
/// </summary>
public interface IDeterministicFunction
{
    /// <summary>
    /// 执行确定性逻辑
    /// </summary>
    /// <param name="frame">当前帧号</param>
    /// <param name="input">输入数据</param>
    /// <param name="random">确定性随机数生成器</param>
    /// <returns>执行结果</returns>
    byte[] Execute(int frame, byte[] input, DeterministicRandom random);

    /// <summary>
    /// 获取当前状态的哈希值
    /// </summary>
    /// <returns>状态哈希</returns>
    int GetStateHash();
}

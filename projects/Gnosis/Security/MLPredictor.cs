using Gnosis.ECS.Core;

namespace Gnosis.Security;

/// <summary>
/// ML 预测器抽象基类，定义机器学习反作弊的推理接口
/// </summary>
public abstract class MLPredictor
{
    #region 属性

    /// <summary>
    /// 获取模型名称
    /// </summary>
    public abstract string ModelName { get; }

    /// <summary>
    /// 获取模型版本
    /// </summary>
    public abstract string ModelVersion { get; }

    #endregion

    #region 公开方法

    /// <summary>
    /// 预测玩家行为序列的作弊概率
    /// </summary>
    /// <param name="sequence">玩家行为序列</param>
    /// <returns>作弊概率，范围 0.0~1.0</returns>
    public abstract float PredictCheatProbability(PlayerActionSequence sequence);

    /// <summary>
    /// 从字节数组加载模型
    /// </summary>
    /// <param name="modelData">模型数据</param>
    public abstract void LoadModel(byte[] modelData);

    #endregion
}

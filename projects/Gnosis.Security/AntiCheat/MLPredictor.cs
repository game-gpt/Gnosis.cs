using Gnosis.Core;

namespace Gnosis.Security.AntiCheat;

/// <summary>
/// ML 预测器抽象基类，定义机器学习反作弊的推理接口
/// </summary>
public abstract class MLPredictor
{
    #region 属性

    public abstract string ModelName { get; }

    public abstract string ModelVersion { get; }

    #endregion

    #region 公开方法

    public abstract float PredictCheatProbability(PlayerActionSequence sequence);

    public abstract void LoadModel(byte[] modelData);

    #endregion
}

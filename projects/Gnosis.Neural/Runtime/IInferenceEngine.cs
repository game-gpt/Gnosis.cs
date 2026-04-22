namespace Gnosis.Neural.Runtime;

/// <summary>
/// 推理引擎接口，执行神经网络的前向传播
/// </summary>
public interface IInferenceEngine
{
    /// <summary>
    /// 关联的模型名称
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// 是否正在执行推理
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 执行前向传播
    /// </summary>
    ITensor Forward(ITensor input);

    /// <summary>
    /// 批量前向传播
    /// </summary>
    IReadOnlyList<ITensor> ForwardBatch(IReadOnlyList<ITensor> inputs);

    /// <summary>
    /// 重置引擎状态
    /// </summary>
    void Reset();
}

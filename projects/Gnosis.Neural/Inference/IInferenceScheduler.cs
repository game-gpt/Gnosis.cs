namespace Gnosis.Neural.Inference;

/// <summary>
/// 异步推理调度接口，管理多模型并发推理
/// </summary>
public interface IInferenceScheduler
{
    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 待处理推理请求数量
    /// </summary>
    int PendingCount { get; }

    /// <summary>
    /// 提交推理请求
    /// </summary>
    /// <returns>推理请求 ID</returns>
    int Submit(string modelName, float[] input, InferencePriority priority = InferencePriority.Normal);

    /// <summary>
    /// 取消推理请求
    /// </summary>
    bool Cancel(int requestId);

    /// <summary>
    /// 获取推理结果
    /// </summary>
    float[]? GetResult(int requestId);

    /// <summary>
    /// 启动调度器
    /// </summary>
    void Start();

    /// <summary>
    /// 停止调度器
    /// </summary>
    void Stop();

    /// <summary>
    /// 更新调度器（每帧调用）
    /// </summary>
    void Update(float delta);
}

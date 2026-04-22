using Gnosis.Neural.Model;

namespace Gnosis.Neural.Runtime;

/// <summary>
/// 扩散模型运行时接口，提供扩散模型推理的核心能力
/// </summary>
public interface IDiffusionRuntime
{
    /// <summary>
    /// 是否已初始化
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// 加载扩散模型
    /// </summary>
    INeuralModel LoadModel(string modelPath);

    /// <summary>
    /// 执行去噪步骤
    /// </summary>
    void DenoiseStep(float[] latents, float[] noise, float timestep);

    /// <summary>
    /// 初始化运行时
    /// </summary>
    void Initialize();

    /// <summary>
    /// 关闭运行时
    /// </summary>
    void Shutdown();
}

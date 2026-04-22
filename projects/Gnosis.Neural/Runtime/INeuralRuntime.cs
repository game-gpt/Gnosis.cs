using Gnosis.Neural.Graph;
using Gnosis.Neural.Model;

namespace Gnosis.Neural.Runtime;

/// <summary>
/// 神经推理运行时接口，提供神经网络推理的核心能力
/// </summary>
public interface INeuralRuntime
{
    /// <summary>
    /// 是否已初始化
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// 加载模型
    /// </summary>
    INeuralModel LoadModel(string modelPath, QuantizationType quantization = QuantizationType.None);

    /// <summary>
    /// 卸载模型
    /// </summary>
    void UnloadModel(string modelName);

    /// <summary>
    /// 创建推理引擎
    /// </summary>
    IInferenceEngine CreateEngine(INeuralModel model);

    /// <summary>
    /// 销毁推理引擎
    /// </summary>
    void DestroyEngine(IInferenceEngine engine);

    /// <summary>
    /// 获取已加载的模型名称列表
    /// </summary>
    IReadOnlyList<string> LoadedModels { get; }

    /// <summary>
    /// 初始化运行时
    /// </summary>
    void Initialize();

    /// <summary>
    /// 关闭运行时
    /// </summary>
    void Shutdown();
}

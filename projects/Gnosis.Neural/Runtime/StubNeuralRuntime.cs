using Gnosis.Neural.Graph;
using Gnosis.Neural.Model;

namespace Gnosis.Neural.Runtime;

/// <summary>
/// 神经推理运行时占位实现
/// </summary>
public class StubNeuralRuntime : INeuralRuntime
{
    public bool IsInitialized => throw new NotImplementedException("Neural 系统尚未实现");

    public IReadOnlyList<string> LoadedModels => throw new NotImplementedException("Neural 系统尚未实现");

    public IInferenceEngine CreateEngine(INeuralModel model)
    {
        throw new NotImplementedException("Neural 系统尚未实现");
    }

    public void DestroyEngine(IInferenceEngine engine)
    {
        throw new NotImplementedException("Neural 系统尚未实现");
    }

    public INeuralModel LoadModel(string modelPath, QuantizationType quantization = QuantizationType.None)
    {
        throw new NotImplementedException("Neural 系统尚未实现");
    }

    public void UnloadModel(string modelName)
    {
        throw new NotImplementedException("Neural 系统尚未实现");
    }

    public void Initialize()
    {
        throw new NotImplementedException("Neural 系统尚未实现");
    }

    public void Shutdown()
    {
        throw new NotImplementedException("Neural 系统尚未实现");
    }
}

using Gnosis.Neural.Model;

namespace Gnosis.Neural.Runtime;

/// <summary>
/// 推理引擎实现，执行神经网络的前向传播
/// </summary>
public sealed class InferenceEngine : IInferenceEngine
{
    #region 字段

    private readonly string _id;
    private INeuralModel _model;
    private bool _isRunning;
    private long _inferenceCount;

    #endregion

    #region IInferenceEngine 实现

    /// <summary>
    /// 关联的模型名称
    /// </summary>
    public string ModelName => _model.Name;

    /// <summary>
    /// 是否正在执行推理
    /// </summary>
    public bool IsRunning => _isRunning;

    #endregion

    #region 属性

    /// <summary>
    /// 引擎唯一标识
    /// </summary>
    public string Id => _id;

    /// <summary>
    /// 推理次数统计
    /// </summary>
    public long InferenceCount => _inferenceCount;

    /// <summary>
    /// 关联的模型
    /// </summary>
    public INeuralModel Model => _model;

    #endregion

    #region 构造函数

    public InferenceEngine(string modelName, INeuralModel model)
    {
        _id = $"{modelName}_{Guid.NewGuid():N}";
        _model = model;
    }

    #endregion

    #region IInferenceEngine 实现

    /// <summary>
    /// 执行前向传播
    /// </summary>
    public ITensor Forward(ITensor input)
    {
        _isRunning = true;

        try
        {
            var result = ExecuteForward(input);
            _inferenceCount++;
            return result;
        }
        finally
        {
            _isRunning = false;
        }
    }

    /// <summary>
    /// 批量前向传播
    /// </summary>
    public IReadOnlyList<ITensor> ForwardBatch(IReadOnlyList<ITensor> inputs)
    {
        var results = new List<ITensor>();

        foreach (var input in inputs)
        {
            results.Add(Forward(input));
        }

        return results;
    }

    /// <summary>
    /// 重置引擎状态
    /// </summary>
    public void Reset()
    {
        _isRunning = false;
        _inferenceCount = 0;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 模型热替换时更新引用
    /// </summary>
    public void OnModelSwapped(INeuralModel newModel)
    {
        _model = newModel;
    }

    #endregion

    #region 私有方法

    private ITensor ExecuteForward(ITensor input)
    {
        if (_model is not NeuralModel neuralModel)
        {
            return input;
        }

        var graph = _model.Graph;
        var sortedNodes = graph.TopologicalSort();

        var nodeOutputs = new Dictionary<string, Tensor>();

        if (input is Tensor inputTensor)
        {
            nodeOutputs["input"] = inputTensor;
        }
        else
        {
            nodeOutputs["input"] = Tensor.Zeros(input.Shape.Dimensions);
        }

        foreach (var node in sortedNodes)
        {
            if (node.NodeType == NeuralNodeType.Input)
            {
                continue;
            }

            if (node.NodeType == NeuralNodeType.Output)
            {
                if (node.Inputs.Count > 0 && nodeOutputs.TryGetValue(node.Inputs[0].Name, out var output))
                {
                    return output;
                }

                return input;
            }

            var inputTensors = new List<Tensor>();
            foreach (var inputNode in node.Inputs)
            {
                if (nodeOutputs.TryGetValue(inputNode.Name, out var tensor))
                {
                    inputTensors.Add(tensor);
                }
            }

            if (inputTensors.Count == 0)
            {
                continue;
            }

            var result = ExecuteNode(node, inputTensors, neuralModel);
            nodeOutputs[node.Name] = result;
        }

        return input;
    }

    private static Tensor ExecuteNode(INeuralNode node, List<Tensor> inputs, NeuralModel model)
    {
        var input = inputs[0];

        return node.NodeType switch
        {
            NeuralNodeType.Linear => ExecuteLinear(node, input, model),
            NeuralNodeType.Activation => ExecuteActivation(input),
            NeuralNodeType.Normalization => ExecuteNormalization(input),
            NeuralNodeType.Reshape => ExecuteReshape(node, input),
            _ => input
        };
    }

    private static Tensor ExecuteLinear(INeuralNode node, Tensor input, NeuralModel model)
    {
        if (node.Parameters.TryGetValue("weight_name", out var weightNameObj) &&
            weightNameObj is string weightName)
        {
            var weight = model.GetWeight(weightName);
            if (weight is not null)
            {
                var outputLength = weight.Shape.Rank >= 2 ? weight.Shape.Dimensions[0] : input.Length;
                var output = new float[Math.Max(1, outputLength)];

                var weightData = weight.GetData();
                var inputData = input.GetData();

                for (var i = 0; i < output.Length; i++)
                {
                    var sum = 0.0f;
                    for (var j = 0; j < Math.Min(inputData.Length, weightData.Length / Math.Max(1, output.Length)); j++)
                    {
                        var weightIdx = i * (weightData.Length / output.Length) + j;
                        if (weightIdx < weightData.Length && j < inputData.Length)
                        {
                            sum += inputData[j] * weightData[weightIdx];
                        }
                    }

                    output[i] = sum;
                }

                return new Tensor(output, new TensorShape(output.Length));
            }
        }

        return input;
    }

    private static Tensor ExecuteActivation(Tensor input)
    {
        var data = input.GetData();
        var output = new float[data.Length];

        for (var i = 0; i < data.Length; i++)
        {
            output[i] = Math.Max(0, data[i]);
        }

        return new Tensor(output, input.Shape);
    }

    private static Tensor ExecuteNormalization(Tensor input)
    {
        var data = input.GetData();
        var output = new float[data.Length];

        var mean = 0.0f;
        for (var i = 0; i < data.Length; i++)
        {
            mean += data[i];
        }

        mean /= Math.Max(1, data.Length);

        var variance = 0.0f;
        for (var i = 0; i < data.Length; i++)
        {
            var diff = data[i] - mean;
            variance += diff * diff;
        }

        variance /= Math.Max(1, data.Length);

        var stdDev = MathF.Sqrt(variance + 1e-5f);

        for (var i = 0; i < data.Length; i++)
        {
            output[i] = (data[i] - mean) / stdDev;
        }

        return new Tensor(output, input.Shape);
    }

    private static Tensor ExecuteReshape(INeuralNode node, Tensor input)
    {
        return input;
    }

    #endregion
}

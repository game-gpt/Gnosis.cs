using Gnosis.Neural.Graph;
using Gnosis.Neural.Runtime;

namespace Gnosis.Neural.Model;

/// <summary>
/// ONNX 模型加载器，使用 Acorn.SafeTensors 解码模型权重
/// </summary>
public sealed class OnnxModelLoader : IModelLoader
{
    #region 字段

    private readonly List<ModelFormat> _supportedFormats = new() { ModelFormat.Onnx };

    #endregion

    #region IModelLoader 实现

    /// <summary>
    /// 支持的模型格式
    /// </summary>
    public IReadOnlyList<ModelFormat> SupportedFormats => _supportedFormats;

    /// <summary>
    /// 从资产路径加载模型
    /// </summary>
    public INeuralModel Load(string modelPath, QuantizationType quantization = QuantizationType.None)
    {
        var modelName = Path.GetFileNameWithoutExtension(modelPath);
        var model = new NeuralModel(modelName, ModelFormat.Onnx, modelPath, quantization);

        var weightsDir = Path.GetDirectoryName(modelPath) ?? string.Empty;
        var safetensorsPath = FindSafeTensorsFile(weightsDir, modelName);

        if (safetensorsPath is not null)
        {
            LoadWeightsFromSafeTensors(model, safetensorsPath);
        }

        BuildDefaultGraph(model);
        model.SetLoaded(true);
        return model;
    }

    /// <summary>
    /// 卸载模型
    /// </summary>
    public void Unload(INeuralModel model)
    {
        if (model is NeuralModel neuralModel)
        {
            neuralModel.ClearWeights();
            neuralModel.SetLoaded(false);
        }
    }

    /// <summary>
    /// 是否支持指定格式
    /// </summary>
    public bool SupportsFormat(ModelFormat format)
    {
        return format == ModelFormat.Onnx;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 从 SafeTensors 文件重新加载模型权重（热加载核心）
    /// </summary>
    public bool ReloadWeights(NeuralModel model)
    {
        var safetensorsPath = FindSafeTensorsFile(
            Path.GetDirectoryName(model.FilePath) ?? string.Empty,
            model.Name);

        if (safetensorsPath is null)
        {
            return false;
        }

        model.ClearWeights();
        LoadWeightsFromSafeTensors(model, safetensorsPath);
        model.IncrementVersion();
        return true;
    }

    /// <summary>
    /// 从指定路径重新加载模型权重
    /// </summary>
    public bool ReloadWeightsFromPath(NeuralModel model, string safetensorsPath)
    {
        if (!File.Exists(safetensorsPath))
        {
            return false;
        }

        model.ClearWeights();
        LoadWeightsFromSafeTensors(model, safetensorsPath);
        model.IncrementVersion();
        return true;
    }

    #endregion

    #region 私有方法

    private static string? FindSafeTensorsFile(string directory, string modelName)
    {
        var candidates = new[]
        {
            Path.Combine(directory, $"{modelName}.safetensors"),
            Path.Combine(directory, "model.safetensors"),
            Path.Combine(directory, "weights.safetensors"),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static void LoadWeightsFromSafeTensors(NeuralModel model, string safetensorsPath)
    {
        using var stream = File.OpenRead(safetensorsPath);
        var decoder = new Acorn.SafeTensors.Decode.SafeTensorsDecoder();
        var fileData = decoder.Decode(stream);

        foreach (var tensorMeta in fileData.Tensors)
        {
            var shape = new TensorShape(tensorMeta.Shape);
            var dataType = MapDataType(tensorMeta.DataType);

            var tensor = new Tensor(shape, dataType);

            for (var i = 0; i < Math.Min(tensor.Length, tensorMeta.Data.Length / sizeof(float)); i++)
            {
                var offset = i * sizeof(float);
                if (offset + sizeof(float) <= tensorMeta.Data.Length)
                {
                    tensor.SetFloat(i, BitConverter.ToSingle(tensorMeta.Data.Span.Slice(offset, sizeof(float))));
                }
            }

            model.AddWeight(tensorMeta.Name, tensor);
        }
    }

    private static TensorDataType MapDataType(string dataType)
    {
        return dataType switch
        {
            "F32" or "FLOAT32" => TensorDataType.Float32,
            "F16" or "FLOAT16" => TensorDataType.Float16,
            "BF16" or "BFLOAT16" => TensorDataType.BFloat16,
            "I8" or "INT8" => TensorDataType.Int8,
            "I32" or "INT32" => TensorDataType.Int32,
            "U8" or "UINT8" => TensorDataType.UInt8,
            _ => TensorDataType.Float32
        };
    }

    private static void BuildDefaultGraph(NeuralModel model)
    {
        var graph = new NeuralGraph(model.Name);

        var inputNode = new NeuralNode("input", NeuralNodeType.Input);
        graph.AddNode(inputNode);

        var outputNode = new NeuralNode("output", NeuralNodeType.Output);
        graph.AddNode(outputNode);

        var weightIndex = 0;
        foreach (var weightName in model.Weights.Keys)
        {
            var layerNode = new NeuralNode($"layer_{weightIndex}_{weightName}", NeuralNodeType.Linear);
            layerNode.SetParameter("weight_name", weightName);
            graph.AddNode(layerNode);

            if (weightIndex == 0)
            {
                graph.Connect(inputNode, layerNode);
            }

            if (weightIndex > 0)
            {
                var prevNodes = graph.Nodes.Where(n => n.NodeType == NeuralNodeType.Linear).ToList();
                if (prevNodes.Count > 1)
                {
                    graph.Connect(prevNodes[^2], layerNode);
                }
            }

            weightIndex++;
        }

        var linearNodes = graph.Nodes.Where(n => n.NodeType == NeuralNodeType.Linear).ToList();
        if (linearNodes.Count > 0)
        {
            graph.Connect(linearNodes[^1], outputNode);
        }
        else
        {
            graph.Connect(inputNode, outputNode);
        }

        model.SetGraph(graph);
    }

    #endregion
}

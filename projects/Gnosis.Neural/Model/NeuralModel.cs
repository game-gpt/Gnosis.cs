using Gnosis.Neural.Graph;

namespace Gnosis.Neural.Model;

/// <summary>
/// 神经网络模型实现
/// </summary>
public sealed class NeuralModel : INeuralModel
{
    #region 字段

    private INeuralGraph _graph;
    private bool _isLoaded;
    private long _parameterCount;
    private readonly Dictionary<string, Runtime.Tensor> _weights = new();

    #endregion

    #region INeuralModel 实现

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 模型格式
    /// </summary>
    public ModelFormat Format { get; }

    /// <summary>
    /// 量化类型
    /// </summary>
    public QuantizationType Quantization { get; }

    /// <summary>
    /// 计算图
    /// </summary>
    public INeuralGraph Graph => _graph;

    /// <summary>
    /// 是否已加载
    /// </summary>
    public bool IsLoaded => _isLoaded;

    /// <summary>
    /// 获取模型参数数量
    /// </summary>
    public long ParameterCount => _parameterCount;

    #endregion

    #region 属性

    /// <summary>
    /// 模型权重张量
    /// </summary>
    public IReadOnlyDictionary<string, Runtime.Tensor> Weights => _weights;

    /// <summary>
    /// 模型文件路径
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// 模型版本号（用于热加载追踪）
    /// </summary>
    public long Version { get; private set; }

    #endregion

    #region 构造函数

    public NeuralModel(string name, ModelFormat format, string filePath, QuantizationType quantization = QuantizationType.None)
    {
        Name = name;
        Format = format;
        FilePath = filePath;
        Quantization = quantization;
        _graph = new NeuralGraph(name);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 设置计算图
    /// </summary>
    public void SetGraph(INeuralGraph graph)
    {
        _graph = graph;
    }

    /// <summary>
    /// 标记模型为已加载
    /// </summary>
    public void SetLoaded(bool loaded)
    {
        _isLoaded = loaded;
    }

    /// <summary>
    /// 设置参数数量
    /// </summary>
    public void SetParameterCount(long count)
    {
        _parameterCount = count;
    }

    /// <summary>
    /// 添加权重张量
    /// </summary>
    public void AddWeight(string name, Runtime.Tensor tensor)
    {
        _weights[name] = tensor;
        _parameterCount += tensor.Length;
    }

    /// <summary>
    /// 移除权重张量
    /// </summary>
    public void RemoveWeight(string name)
    {
        if (_weights.TryGetValue(name, out var tensor))
        {
            _parameterCount -= tensor.Length;
            _weights.Remove(name);
        }
    }

    /// <summary>
    /// 获取权重张量
    /// </summary>
    public Runtime.Tensor? GetWeight(string name)
    {
        return _weights.GetValueOrDefault(name);
    }

    /// <summary>
    /// 清空所有权重
    /// </summary>
    public void ClearWeights()
    {
        _weights.Clear();
        _parameterCount = 0;
    }

    /// <summary>
    /// 递增版本号（热加载时调用）
    /// </summary>
    public void IncrementVersion()
    {
        Version++;
    }

    /// <summary>
    /// 从另一个模型替换权重和计算图（热加载核心操作）
    /// </summary>
    public void ReplaceFrom(NeuralModel source)
    {
        _weights.Clear();

        foreach (var kvp in source.Weights)
        {
            _weights[kvp.Key] = kvp.Value;
        }

        _graph = source.Graph;
        _parameterCount = source.ParameterCount;
        IncrementVersion();
    }

    #endregion
}

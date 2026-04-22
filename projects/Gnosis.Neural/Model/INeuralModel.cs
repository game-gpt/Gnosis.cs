using Gnosis.Neural.Graph;

namespace Gnosis.Neural.Model;

/// <summary>
/// 神经网络模型接口
/// </summary>
public interface INeuralModel
{
    /// <summary>
    /// 模型名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 模型格式
    /// </summary>
    ModelFormat Format { get; }

    /// <summary>
    /// 量化类型
    /// </summary>
    QuantizationType Quantization { get; }

    /// <summary>
    /// 计算图
    /// </summary>
    INeuralGraph Graph { get; }

    /// <summary>
    /// 是否已加载
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// 获取模型参数数量
    /// </summary>
    long ParameterCount { get; }
}

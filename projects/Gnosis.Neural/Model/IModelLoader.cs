namespace Gnosis.Neural.Model;

/// <summary>
/// 模型加载器接口
/// </summary>
public interface IModelLoader
{
    /// <summary>
    /// 支持的模型格式
    /// </summary>
    IReadOnlyList<ModelFormat> SupportedFormats { get; }

    /// <summary>
    /// 从资产路径加载模型
    /// </summary>
    INeuralModel Load(string modelPath, QuantizationType quantization = QuantizationType.None);

    /// <summary>
    /// 卸载模型
    /// </summary>
    void Unload(INeuralModel model);

    /// <summary>
    /// 是否支持指定格式
    /// </summary>
    bool SupportsFormat(ModelFormat format);
}

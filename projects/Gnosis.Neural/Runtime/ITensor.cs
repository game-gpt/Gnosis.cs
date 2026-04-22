namespace Gnosis.Neural.Runtime;

/// <summary>
/// 张量接口，提供对神经网络张量数据的访问
/// </summary>
public interface ITensor
{
    /// <summary>
    /// 张量形状
    /// </summary>
    TensorShape Shape { get; }

    /// <summary>
    /// 张量数据类型
    /// </summary>
    TensorDataType DataType { get; }

    /// <summary>
    /// 元素总数
    /// </summary>
    int Length { get; }

    /// <summary>
    /// 获取原始数据
    /// </summary>
    ReadOnlySpan<byte> GetRawData();

    /// <summary>
    /// 获取指定索引的浮点值
    /// </summary>
    float GetFloat(int index);

    /// <summary>
    /// 设置指定索引的浮点值
    /// </summary>
    void SetFloat(int index, float value);
}

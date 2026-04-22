namespace Gnosis.Neural.Runtime;

/// <summary>
/// 张量形状，描述张量的维度信息
/// </summary>
public readonly record struct TensorShape
{
    /// <summary>
    /// 维度数组
    /// </summary>
    public int[] Dimensions { get; init; }

    /// <summary>
    /// 维度数量
    /// </summary>
    public int Rank => Dimensions.Length;

    /// <summary>
    /// 元素总数
    /// </summary>
    public int ElementCount
    {
        get
        {
            var count = 1;
            for (var i = 0; i < Dimensions.Length; i++)
            {
                count *= Dimensions[i];
            }
            return count;
        }
    }

    public TensorShape(params int[] dimensions)
    {
        Dimensions = dimensions;
    }
}

namespace Gnosis.Rendering.Backends.ShaderIR;

/// <summary>
/// 张量操作指令，用于 Tensor Core / Cooperative Matrix 执行路径
/// </summary>
public sealed record TensorInstruction(
    TensorOpCode OpCode,
    ShaderIrType? ResultType,
    uint[] OperandIds) : ShaderIrInstruction(ShaderIrOpCode.Nop)
{
    public override ShaderIrType? ResultType { get; init; } = ResultType;
}

/// <summary>
/// 张量操作码枚举
/// </summary>
public enum TensorOpCode
{
    /// <summary>
    /// 矩阵乘法，映射为 Tensor Core MMA 指令
    /// </summary>
    MatMul = 0,

    /// <summary>
    /// 2D 卷积，映射为 Winograd / im2col + matmul
    /// </summary>
    Conv2D = 1,

    /// <summary>
    /// 张量重塑（零拷贝视图变换）
    /// </summary>
    Reshape = 2,

    /// <summary>
    /// 维度转置
    /// </summary>
    Transpose = 3,

    /// <summary>
    /// 跨维度归约求和
    /// </summary>
    ReduceSum = 4,

    /// <summary>
    /// 数值稳定 softmax
    /// </summary>
    Softmax = 5,

    /// <summary>
    /// 沿指定维度拼接
    /// </summary>
    Concat = 6,

    /// <summary>
    /// 像素重排（用于超分辨率）
    /// </summary>
    PixelShuffle = 7,

    /// <summary>
    /// 矩阵加偏置（广播加法）
    /// </summary>
    BiasAdd = 8,

    /// <summary>
    /// 激活函数（relu/sigmoid/gelu/silu）
    /// </summary>
    Activate = 9
}

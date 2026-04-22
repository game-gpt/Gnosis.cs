using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// 张量操作码枚举（已迁移至 Gnosis.IR.Shader）
/// </summary>
public enum TensorOpCode
{
    MatMul,
    Conv2D,
    Conv3D,
    Pooling,
    Activation,
    BatchNorm,
    LayerNorm,
    Softmax,
    Attention,
    TransformerBlock,
    Reshape,
    Transpose,
    Concat,
    Split,
    Gather,
    Scatter,
    ReduceSum,
    ReduceMean,
    ReduceMax,
    ReduceMin,
    ElementWiseAdd,
    ElementWiseMul,
    ElementWiseDiv,
    ElementWiseSub,
    ElementWisePow,
    ElementWiseSqrt,
    ElementWiseExp,
    ElementWiseLog,
    ElementWiseAbs,
    ElementWiseSign,
    ElementWiseClamp,
    ElementWiseRelu,
    ElementWiseGelu,
    ElementWiseSilu,
    ElementWiseTanh,
    ElementWiseSigmoid,
    Quantize,
    Dequantize,
    Cast,
    Pad,
    Crop,
    Resize,
    Flip,
    Rotate,
    Custom
}

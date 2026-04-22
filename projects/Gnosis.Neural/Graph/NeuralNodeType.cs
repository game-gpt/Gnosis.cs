namespace Gnosis.Neural.Graph;

/// <summary>
/// 神经网络节点类型
/// </summary>
public enum NeuralNodeType : byte
{
    Input = 0,
    Output = 1,
    Linear = 2,
    Conv2D = 3,
    Activation = 4,
    Normalization = 5,
    Pooling = 6,
    Reshape = 7,
    Custom = 8
}

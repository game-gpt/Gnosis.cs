namespace Gnosis.Neural.Graph;

/// <summary>
/// 神经网络节点接口，描述计算图中的单个操作
/// </summary>
public interface INeuralNode
{
    /// <summary>
    /// 节点名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 节点类型
    /// </summary>
    NeuralNodeType NodeType { get; }

    /// <summary>
    /// 输入节点列表
    /// </summary>
    IReadOnlyList<INeuralNode> Inputs { get; }

    /// <summary>
    /// 输出节点列表
    /// </summary>
    IReadOnlyList<INeuralNode> Outputs { get; }

    /// <summary>
    /// 节点参数（权重名称到张量的映射）
    /// </summary>
    IReadOnlyDictionary<string, object> Parameters { get; }
}

namespace Gnosis.Neural.Graph;

/// <summary>
/// 神经网络图接口，描述神经网络的拓扑结构
/// </summary>
public interface INeuralGraph
{
    /// <summary>
    /// 图名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 所有节点
    /// </summary>
    IReadOnlyList<INeuralNode> Nodes { get; }

    /// <summary>
    /// 输入节点
    /// </summary>
    IReadOnlyList<INeuralNode> InputNodes { get; }

    /// <summary>
    /// 输出节点
    /// </summary>
    IReadOnlyList<INeuralNode> OutputNodes { get; }

    /// <summary>
    /// 添加节点
    /// </summary>
    void AddNode(INeuralNode node);

    /// <summary>
    /// 移除节点
    /// </summary>
    void RemoveNode(string nodeName);

    /// <summary>
    /// 连接两个节点
    /// </summary>
    void Connect(INeuralNode from, INeuralNode to);

    /// <summary>
    /// 获取指定名称的节点
    /// </summary>
    INeuralNode? GetNode(string nodeName);

    /// <summary>
    /// 拓扑排序
    /// </summary>
    IReadOnlyList<INeuralNode> TopologicalSort();
}

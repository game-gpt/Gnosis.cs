namespace Gnosis.Neural.Graph;

/// <summary>
/// 神经网络计算图实现
/// </summary>
public sealed class NeuralGraph : INeuralGraph
{
    #region 字段

    private readonly List<INeuralNode> _nodes = new();
    private readonly List<INeuralNode> _inputNodes = new();
    private readonly List<INeuralNode> _outputNodes = new();
    private readonly Dictionary<string, INeuralNode> _nodeByName = new();

    #endregion

    #region INeuralGraph 实现

    /// <summary>
    /// 图名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 所有节点
    /// </summary>
    public IReadOnlyList<INeuralNode> Nodes => _nodes;

    /// <summary>
    /// 输入节点
    /// </summary>
    public IReadOnlyList<INeuralNode> InputNodes => _inputNodes;

    /// <summary>
    /// 输出节点
    /// </summary>
    public IReadOnlyList<INeuralNode> OutputNodes => _outputNodes;

    #endregion

    #region 构造函数

    public NeuralGraph(string name)
    {
        Name = name;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 添加节点
    /// </summary>
    public void AddNode(INeuralNode node)
    {
        _nodes.Add(node);
        _nodeByName[node.Name] = node;

        if (node.NodeType == NeuralNodeType.Input)
        {
            _inputNodes.Add(node);
        }
        else if (node.NodeType == NeuralNodeType.Output)
        {
            _outputNodes.Add(node);
        }
    }

    /// <summary>
    /// 移除节点
    /// </summary>
    public void RemoveNode(string nodeName)
    {
        var node = GetNode(nodeName);
        if (node is null)
        {
            return;
        }

        _nodes.Remove(node);
        _nodeByName.Remove(nodeName);
        _inputNodes.Remove(node);
        _outputNodes.Remove(node);
    }

    /// <summary>
    /// 连接两个节点
    /// </summary>
    public void Connect(INeuralNode from, INeuralNode to)
    {
        if (from is NeuralNode fromNode)
        {
            fromNode.AddOutput(to);
        }

        if (to is NeuralNode toNode)
        {
            toNode.AddInput(from);
        }
    }

    /// <summary>
    /// 获取指定名称的节点
    /// </summary>
    public INeuralNode? GetNode(string nodeName)
    {
        return _nodeByName.GetValueOrDefault(nodeName);
    }

    /// <summary>
    /// 拓扑排序
    /// </summary>
    public IReadOnlyList<INeuralNode> TopologicalSort()
    {
        var result = new List<INeuralNode>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var node in _nodes)
        {
            VisitNode(node, visited, visiting, result);
        }

        return result;
    }

    #endregion

    #region 私有方法

    private static void VisitNode(INeuralNode node, HashSet<string> visited, HashSet<string> visiting, List<INeuralNode> result)
    {
        if (visited.Contains(node.Name))
        {
            return;
        }

        if (visiting.Contains(node.Name))
        {
            return;
        }

        visiting.Add(node.Name);

        foreach (var input in node.Inputs)
        {
            VisitNode(input, visited, visiting, result);
        }

        visiting.Remove(node.Name);
        visited.Add(node.Name);
        result.Add(node);
    }

    #endregion
}

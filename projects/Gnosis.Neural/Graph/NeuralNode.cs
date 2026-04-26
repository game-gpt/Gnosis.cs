namespace Gnosis.Neural.Graph;

/// <summary>
/// 神经网络计算图节点实现
/// </summary>
public sealed class NeuralNode : INeuralNode
{
    #region 字段

    private readonly List<INeuralNode> _inputs = new();
    private readonly List<INeuralNode> _outputs = new();
    private readonly Dictionary<string, object> _parameters = new();

    #endregion

    #region INeuralNode 实现

    /// <summary>
    /// 节点名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 节点类型
    /// </summary>
    public NeuralNodeType NodeType { get; }

    /// <summary>
    /// 输入节点列表
    /// </summary>
    public IReadOnlyList<INeuralNode> Inputs => _inputs;

    /// <summary>
    /// 输出节点列表
    /// </summary>
    public IReadOnlyList<INeuralNode> Outputs => _outputs;

    /// <summary>
    /// 节点参数
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters => _parameters;

    #endregion

    #region 构造函数

    public NeuralNode(string name, NeuralNodeType nodeType)
    {
        Name = name;
        NodeType = nodeType;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 添加输入节点
    /// </summary>
    public void AddInput(INeuralNode node)
    {
        _inputs.Add(node);
    }

    /// <summary>
    /// 添加输出节点
    /// </summary>
    public void AddOutput(INeuralNode node)
    {
        _outputs.Add(node);
    }

    /// <summary>
    /// 设置参数
    /// </summary>
    public void SetParameter(string key, object value)
    {
        _parameters[key] = value;
    }

    /// <summary>
    /// 移除参数
    /// </summary>
    public void RemoveParameter(string key)
    {
        _parameters.Remove(key);
    }

    #endregion
}

namespace Gnosis.AI.Behavior;

/// <summary>
/// 行为树节点抽象基类
/// </summary>
public abstract class BTNode : IBTNode
{
    #region 字段

    private BTNodeStatus _status = BTNodeStatus.Failure;

    #endregion

    #region IBTNode 实现

    /// <summary>
    /// 节点名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 节点类型
    /// </summary>
    public abstract BTNodeType NodeType { get; }

    /// <summary>
    /// 当前节点状态
    /// </summary>
    public BTNodeStatus Status => _status;

    /// <summary>
    /// 执行节点逻辑
    /// </summary>
    public BTNodeStatus Execute()
    {
        _status = OnExecute();
        return _status;
    }

    /// <summary>
    /// 重置节点状态
    /// </summary>
    public virtual void Reset()
    {
        _status = BTNodeStatus.Failure;
    }

    #endregion

    #region 构造函数

    protected BTNode(string name)
    {
        Name = name;
    }

    #endregion

    #region 保护方法

    /// <summary>
    /// 子类实现的执行逻辑
    /// </summary>
    protected abstract BTNodeStatus OnExecute();

    #endregion
}

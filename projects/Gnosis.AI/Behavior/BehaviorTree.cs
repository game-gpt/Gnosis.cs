using Gnosis.AI.Blackboard;

namespace Gnosis.AI.Behavior;

/// <summary>
/// 行为树实现类，管理行为树的执行生命周期
/// </summary>
public sealed class BehaviorTree : IBehaviorTree
{
    #region 字段

    private IBTNode? _root;
    private BTNodeStatus _status;
    private bool _isRunning;

    #endregion

    #region IBehaviorTree 实现

    /// <summary>
    /// 行为树名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 根节点
    /// </summary>
    public IBTNode Root => _root ?? throw new InvalidOperationException("行为树根节点未设置");

    /// <summary>
    /// 黑板
    /// </summary>
    public IBlackboard Blackboard { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public BTNodeStatus Status => _status;

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning => _isRunning;

    #endregion

    #region 构造函数

    public BehaviorTree(string name, IBlackboard? blackboard = null)
    {
        Name = name;
        Blackboard = blackboard ?? new Blackboard.Blackboard();
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 设置根节点
    /// </summary>
    public void SetRoot(IBTNode root)
    {
        _root = root;
    }

    /// <summary>
    /// 启动行为树
    /// </summary>
    public void Start()
    {
        if (_root is null)
        {
            return;
        }

        _isRunning = true;
        _status = BTNodeStatus.Running;
    }

    /// <summary>
    /// 停止行为树
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _status = BTNodeStatus.Failure;
        _root?.Reset();
    }

    /// <summary>
    /// 重启行为树
    /// </summary>
    public void Restart()
    {
        Stop();
        Start();
    }

    /// <summary>
    /// 执行一次行为树 Tick
    /// </summary>
    public BTNodeStatus Tick()
    {
        if (!_isRunning || _root is null)
        {
            return _status;
        }

        _status = _root.Execute();

        if (_status != BTNodeStatus.Running)
        {
            _isRunning = false;
        }

        return _status;
    }

    #endregion
}

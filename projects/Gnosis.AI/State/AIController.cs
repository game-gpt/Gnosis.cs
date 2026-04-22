using Gnosis.AI.Behavior;
using Gnosis.AI.Perception;

namespace Gnosis.AI.State;

/// <summary>
/// AI 控制器实现，整合行为树、感知系统和目标选择
/// </summary>
public sealed class AIController : IAIController
{
    #region 字段

    private IBehaviorTree? _behaviorTree;
    private IAIPerception? _perception;

    #endregion

    #region 属性

    /// <summary>
    /// 行为树
    /// </summary>
    public IBehaviorTree? BehaviorTree => _behaviorTree;

    /// <summary>
    /// 感知系统
    /// </summary>
    public IAIPerception? Perception => _perception;

    /// <summary>
    /// 目标选择器
    /// </summary>
    public ITargetSelector TargetSelector { get; }

    /// <summary>
    /// 状态机
    /// </summary>
    public IStateMachine? StateMachine { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建 AI 控制器
    /// </summary>
    public AIController()
    {
        TargetSelector = new TargetSelector();
    }

    /// <summary>
    /// 创建 AI 控制器（使用自定义目标选择器）
    /// </summary>
    /// <param name="targetSelector">目标选择器</param>
    public AIController(ITargetSelector targetSelector)
    {
        TargetSelector = targetSelector;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 设置行为树
    /// </summary>
    /// <param name="tree">行为树</param>
    public void SetBehaviorTree(IBehaviorTree tree)
    {
        _behaviorTree = tree;
    }

    /// <summary>
    /// 设置感知系统
    /// </summary>
    /// <param name="perception">感知系统</param>
    public void SetPerception(IAIPerception perception)
    {
        _perception = perception;
    }

    /// <summary>
    /// 更新 AI 控制器
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void Update(float delta)
    {
        if (!IsEnabled)
        {
            return;
        }

        _perception?.Update(delta);

        UpdateTargetFromPerception();

        TargetSelector.Update(delta);

        StateMachine?.Update(delta);

        if (_behaviorTree is { IsRunning: true })
        {
            _behaviorTree.Tick();
        }
    }

    /// <summary>
    /// 重置 AI 控制器
    /// </summary>
    public void Reset()
    {
        _behaviorTree?.Stop();
        _perception?.Memory.Clear();
        TargetSelector.ClearTarget();
        StateMachine?.Reset();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 从感知系统更新目标
    /// </summary>
    private void UpdateTargetFromPerception()
    {
        if (_perception is null)
        {
            return;
        }

        if (_perception.PerceivedTargets.Count == 0)
        {
            return;
        }

        if (TargetSelector.HasTarget)
        {
            return;
        }

        var firstTarget = _perception.PerceivedTargets[0];
        TargetSelector.SetTarget(firstTarget.Position);
    }

    #endregion
}

namespace Gnosis.AI.Planning;

/// <summary>
/// GOAP 动作实现
/// </summary>
public sealed class GOAPAction : IGOAPAction
{
    #region 字段

    private readonly Func<float, bool> _executeFunc;
    private readonly IWorldState _preconditions;
    private readonly IWorldState _effects;

    #endregion

    #region 属性

    /// <summary>
    /// 动作名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 动作代价
    /// </summary>
    public float Cost { get; set; }

    /// <summary>
    /// 前置条件
    /// </summary>
    public IWorldState Preconditions => _preconditions;

    /// <summary>
    /// 执行效果
    /// </summary>
    public IWorldState Effects => _effects;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建 GOAP 动作
    /// </summary>
    /// <param name="name">动作名称</param>
    /// <param name="cost">动作代价</param>
    /// <param name="preconditions">前置条件</param>
    /// <param name="effects">执行效果</param>
    /// <param name="executeFunc">执行函数</param>
    public GOAPAction(
        string name,
        float cost,
        IWorldState preconditions,
        IWorldState effects,
        Func<float, bool>? executeFunc = null)
    {
        Name = name;
        Cost = cost;
        _preconditions = preconditions;
        _effects = effects;
        _executeFunc = executeFunc ?? (_ => true);
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 是否可在当前世界状态下执行
    /// </summary>
    /// <param name="worldState">当前世界状态</param>
    /// <returns>是否可执行</returns>
    public bool IsExecutable(IWorldState worldState)
    {
        return worldState.Satisfies(_preconditions);
    }

    /// <summary>
    /// 执行动作，返回是否成功
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    /// <returns>是否成功</returns>
    public bool Execute(float delta)
    {
        return _executeFunc(delta);
    }

    #endregion
}

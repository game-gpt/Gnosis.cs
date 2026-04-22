namespace Gnosis.Navigation.Crowd;

/// <summary>
/// 人群管理器实现，管理多个 AI 代理的协同移动与 RVO 避障
/// </summary>
public sealed class CrowdManager : ICrowdManager
{
    #region 字段

    private readonly Dictionary<int, CrowdAgent> _agents = new();
    private int _nextAgentId;

    #endregion

    #region 属性

    /// <summary>
    /// 代理数量
    /// </summary>
    public int AgentCount => _agents.Count;

    /// <summary>
    /// 最大代理数量
    /// </summary>
    public int MaxAgents { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建人群管理器
    /// </summary>
    /// <param name="maxAgents">最大代理数量</param>
    public CrowdManager(int maxAgents = 100)
    {
        MaxAgents = maxAgents;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 添加代理
    /// </summary>
    /// <param name="position">初始位置</param>
    /// <param name="parameters">代理参数</param>
    /// <returns>代理实例</returns>
    public ICrowdAgent AddAgent(float[] position, CrowdAgentParams parameters)
    {
        if (_agents.Count >= MaxAgents)
        {
            throw new InvalidOperationException($"已达到最大代理数量限制: {MaxAgents}");
        }

        int id = _nextAgentId++;
        var agent = new CrowdAgent(id, position, parameters);
        _agents[id] = agent;

        return agent;
    }

    /// <summary>
    /// 移除代理
    /// </summary>
    /// <param name="agentId">代理 ID</param>
    public void RemoveAgent(int agentId)
    {
        _agents.Remove(agentId);
    }

    /// <summary>
    /// 获取代理
    /// </summary>
    /// <param name="agentId">代理 ID</param>
    /// <returns>代理实例，不存在则返回 null</returns>
    public ICrowdAgent? GetAgent(int agentId)
    {
        return _agents.GetValueOrDefault(agentId);
    }

    /// <summary>
    /// 更新人群模拟
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void Update(float delta)
    {
        var agentList = _agents.Values.ToList().AsReadOnly();

        foreach (var agent in agentList)
        {
            agent.ComputeDesiredVelocity();
        }

        foreach (var agent in agentList)
        {
            agent.ApplyRVO(agentList);
        }

        foreach (var agent in agentList)
        {
            agent.Integrate(delta);
        }
    }

    /// <summary>
    /// 获取所有代理
    /// </summary>
    /// <returns>代理列表</returns>
    public IReadOnlyList<ICrowdAgent> GetAgents()
    {
        return _agents.Values.Cast<ICrowdAgent>().ToList().AsReadOnly();
    }

    #endregion
}

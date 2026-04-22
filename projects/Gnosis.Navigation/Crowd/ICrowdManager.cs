namespace Gnosis.Navigation.Crowd;

/// <summary>
/// 人群管理器接口，管理多个 AI 代理的协同移动与 RVO 避障
/// </summary>
public interface ICrowdManager
{
    /// <summary>
    /// 代理数量
    /// </summary>
    int AgentCount { get; }

    /// <summary>
    /// 最大代理数量
    /// </summary>
    int MaxAgents { get; }

    /// <summary>
    /// 添加代理
    /// </summary>
    ICrowdAgent AddAgent(float[] position, CrowdAgentParams parameters);

    /// <summary>
    /// 移除代理
    /// </summary>
    void RemoveAgent(int agentId);

    /// <summary>
    /// 获取代理
    /// </summary>
    ICrowdAgent? GetAgent(int agentId);

    /// <summary>
    /// 更新人群模拟
    /// </summary>
    void Update(float delta);

    /// <summary>
    /// 获取所有代理
    /// </summary>
    IReadOnlyList<ICrowdAgent> GetAgents();
}

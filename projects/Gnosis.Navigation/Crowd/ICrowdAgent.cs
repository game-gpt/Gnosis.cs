using Gnosis.Core.Math;

namespace Gnosis.Navigation.Crowd;

/// <summary>
/// 人群代理接口，描述单个在人群中移动的 AI 代理
/// </summary>
public interface ICrowdAgent
{
    /// <summary>
    /// 代理 ID
    /// </summary>
    int Id { get; }

    /// <summary>
    /// 当前位置
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// 目标位置
    /// </summary>
    Vector3 Target { get; }

    /// <summary>
    /// 当前速度
    /// </summary>
    Vector3 Velocity { get; }

    /// <summary>
    /// 代理参数
    /// </summary>
    CrowdAgentParams Params { get; set; }

    /// <summary>
    /// 是否已到达目标
    /// </summary>
    bool HasReachedTarget { get; }

    /// <summary>
    /// 设置目标位置
    /// </summary>
    void SetTarget(Vector3 target);

    /// <summary>
    /// 重置代理状态
    /// </summary>
    void Reset();
}

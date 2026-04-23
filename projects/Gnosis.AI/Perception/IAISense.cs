using Gnosis.Core.Math;

namespace Gnosis.AI.Perception;

/// <summary>
/// AI 感知通道接口，定义感知通道的基本能力
/// </summary>
public interface IAISense
{
    /// <summary>
    /// 感知类型
    /// </summary>
    AISenseType SenseType { get; }

    /// <summary>
    /// 当前感知到的目标列表
    /// </summary>
    IReadOnlyList<IAIStimulusSource> PerceivedTargets { get; }

    /// <summary>
    /// 感知者位置
    /// </summary>
    Vector3 OwnerPosition { get; set; }

    /// <summary>
    /// 更新感知通道
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    void Update(float delta);

    /// <summary>
    /// 注册可感知目标
    /// </summary>
    /// <param name="source">刺激源</param>
    void RegisterTarget(IAIStimulusSource source);

    /// <summary>
    /// 注销可感知目标
    /// </summary>
    /// <param name="source">刺激源</param>
    void UnregisterTarget(IAIStimulusSource source);
}

/// <summary>
/// AI 刺激源接口，表示世界中可被 AI 感知的对象
/// </summary>
public interface IAIStimulusSource
{
    /// <summary>
    /// 刺激源位置
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// 刺激强度，0 表示无刺激
    /// </summary>
    float Strength { get; }

    /// <summary>
    /// 刺激类型
    /// </summary>
    AISenseType SenseType { get; }

    /// <summary>
    /// 刺激源是否激活
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 刺激源半径，用于触觉感知的碰撞检测
    /// </summary>
    float Radius { get; }
}

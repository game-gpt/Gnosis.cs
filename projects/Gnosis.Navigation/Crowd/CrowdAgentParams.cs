namespace Gnosis.Navigation.Crowd;

/// <summary>
/// 人群代理参数
/// </summary>
public struct CrowdAgentParams
{
    /// <summary>
    /// 代理半径
    /// </summary>
    public float Radius { get; init; }

    /// <summary>
    /// 代理高度
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    /// 最大速度
    /// </summary>
    public float MaxSpeed { get; init; }

    /// <summary>
    /// 最大加速度
    /// </summary>
    public float MaxAcceleration { get; init; }

    /// <summary>
    /// 避障权重
    /// </summary>
    public float SeparationWeight { get; init; }

    /// <summary>
    /// 路径查询优化类型
    /// </summary>
    public int AreaMask { get; init; }
}

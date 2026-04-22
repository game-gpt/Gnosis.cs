using Gnosis.Navigation.Query;

namespace Gnosis.Navigation.Path;

/// <summary>
/// 路径请求实现类
/// </summary>
public sealed class PathRequest : IPathRequest
{
    #region IPathRequest 实现

    /// <summary>
    /// 起始点
    /// </summary>
    public float[] Start { get; }

    /// <summary>
    /// 终止点
    /// </summary>
    public float[] End { get; }

    /// <summary>
    /// 可通行区域掩码
    /// </summary>
    public int AreaMask { get; }

    /// <summary>
    /// 代价倍率
    /// </summary>
    public float CostMultiplier { get; }

    #endregion

    #region 构造函数

    public PathRequest(float[] start, float[] end, int areaMask = -1, float costMultiplier = 1.0f)
    {
        Start = start;
        End = end;
        AreaMask = areaMask;
        CostMultiplier = costMultiplier;
    }

    #endregion
}

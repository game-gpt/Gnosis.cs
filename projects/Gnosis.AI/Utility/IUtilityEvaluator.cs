namespace Gnosis.AI.Utility;

/// <summary>
/// 效用评估器接口
/// </summary>
public interface IUtilityEvaluator
{
    /// <summary>
    /// 评估器名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 曲线类型
    /// </summary>
    UtilityCurveType CurveType { get; }

    /// <summary>
    /// 评估效用值
    /// </summary>
    float Evaluate(float input);

    /// <summary>
    /// 效用权重
    /// </summary>
    float Weight { get; set; }
}

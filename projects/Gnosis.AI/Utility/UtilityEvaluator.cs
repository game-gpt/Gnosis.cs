namespace Gnosis.AI.Utility;

/// <summary>
/// 效用评估器实现，支持多种曲线类型
/// </summary>
public sealed class UtilityEvaluator : IUtilityEvaluator
{
    #region 字段

    private readonly Func<float, float>? _customCurve;

    #endregion

    #region 属性

    /// <summary>
    /// 评估器名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 曲线类型
    /// </summary>
    public UtilityCurveType CurveType { get; }

    /// <summary>
    /// 效用权重
    /// </summary>
    public float Weight { get; set; } = 1.0f;

    /// <summary>
    /// 曲线参数：最小值
    /// </summary>
    public float MinValue { get; set; } = 0.0f;

    /// <summary>
    /// 曲线参数：最大值
    /// </summary>
    public float MaxValue { get; set; } = 1.0f;

    /// <summary>
    /// 曲线参数：陡峭度（用于 Logistic 曲线）
    /// </summary>
    public float Steepness { get; set; } = 5.0f;

    /// <summary>
    /// 曲线参数：中点（用于 Logistic 曲线）
    /// </summary>
    public float Midpoint { get; set; } = 0.5f;

    /// <summary>
    /// 曲线参数：阈值（用于 Step 曲线）
    /// </summary>
    public float Threshold { get; set; } = 0.5f;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建效用评估器
    /// </summary>
    /// <param name="name">评估器名称</param>
    /// <param name="curveType">曲线类型</param>
    /// <param name="customCurve">自定义曲线函数</param>
    public UtilityEvaluator(string name, UtilityCurveType curveType, Func<float, float>? customCurve = null)
    {
        Name = name;
        CurveType = curveType;
        _customCurve = customCurve;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 评估效用值
    /// </summary>
    /// <param name="input">输入值 [0, 1]</param>
    /// <returns>效用值 [0, 1]</returns>
    public float Evaluate(float input)
    {
        float clamped = Math.Clamp(input, 0.0f, 1.0f);

        float raw = CurveType switch
        {
            UtilityCurveType.Linear => EvaluateLinear(clamped),
            UtilityCurveType.Quadratic => EvaluateQuadratic(clamped),
            UtilityCurveType.Logistic => EvaluateLogistic(clamped),
            UtilityCurveType.Step => EvaluateStep(clamped),
            UtilityCurveType.Custom => _customCurve?.Invoke(clamped) ?? clamped,
            _ => clamped
        };

        float weighted = raw * Weight;

        return Math.Clamp(weighted, MinValue, MaxValue);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 线性曲线评估
    /// </summary>
    private static float EvaluateLinear(float input)
    {
        return input;
    }

    /// <summary>
    /// 二次曲线评估
    /// </summary>
    private static float EvaluateQuadratic(float input)
    {
        return input * input;
    }

    /// <summary>
    /// 逻辑曲线评估
    /// </summary>
    private float EvaluateLogistic(float input)
    {
        float exponent = Steepness * (input - Midpoint);
        return 1.0f / (1.0f + MathF.Exp(-exponent));
    }

    /// <summary>
    /// 阶梯曲线评估
    /// </summary>
    private float EvaluateStep(float input)
    {
        return input >= Threshold ? 1.0f : 0.0f;
    }

    #endregion
}

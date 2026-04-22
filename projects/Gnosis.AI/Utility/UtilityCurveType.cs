namespace Gnosis.AI.Utility;

/// <summary>
/// 效用曲线类型
/// </summary>
public enum UtilityCurveType : byte
{
    Linear = 0,
    Quadratic = 1,
    Logistic = 2,
    Step = 3,
    Custom = 4
}

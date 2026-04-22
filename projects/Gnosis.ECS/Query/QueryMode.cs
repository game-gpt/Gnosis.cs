namespace Gnosis.ECS.Query;

/// <summary>
/// 查询模式枚举
/// </summary>
public enum QueryMode
{
    /// <summary>
    /// 必须包含所有指定类型
    /// </summary>
    All,

    /// <summary>
    /// 包含任意指定类型
    /// </summary>
    Any,

    /// <summary>
    /// 不包含指定类型
    /// </summary>
    None
}

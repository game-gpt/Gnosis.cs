namespace Gnosis.Plugin.Dependency;

/// <summary>
/// 依赖解析结果
/// </summary>
public sealed class DependencyResolveResult
{
    #region 属性

    /// <summary>
    /// 是否解析成功
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 拓扑排序后的加载顺序
    /// </summary>
    public IReadOnlyList<string> LoadOrder { get; }

    /// <summary>
    /// 检测到的循环依赖
    /// </summary>
    public IReadOnlyList<DependencyCycle>? Cycles { get; }

    /// <summary>
    /// 缺失的依赖列表
    /// </summary>
    public IReadOnlyList<MissingDependency>? MissingDependencies { get; }

    /// <summary>
    /// 版本冲突列表
    /// </summary>
    public IReadOnlyList<VersionConflict>? VersionConflicts { get; }

    /// <summary>
    /// 错误描述
    /// </summary>
    public string? ErrorMessage { get; }

    #endregion

    #region 构造函数

    private DependencyResolveResult(
        bool isSuccess,
        IReadOnlyList<string> loadOrder,
        IReadOnlyList<DependencyCycle>? cycles,
        IReadOnlyList<MissingDependency>? missing,
        IReadOnlyList<VersionConflict>? conflicts,
        string? errorMessage)
    {
        IsSuccess = isSuccess;
        LoadOrder = loadOrder;
        Cycles = cycles;
        MissingDependencies = missing;
        VersionConflicts = conflicts;
        ErrorMessage = errorMessage;
    }

    #endregion

    #region 工厂方法

    /// <summary>
    /// 创建成功的解析结果
    /// </summary>
    public static DependencyResolveResult Success(IReadOnlyList<string> loadOrder)
    {
        return new DependencyResolveResult(true, loadOrder, null, null, null, null);
    }

    /// <summary>
    /// 创建失败的解析结果
    /// </summary>
    public static DependencyResolveResult Failure(
        IReadOnlyList<DependencyCycle>? cycles = null,
        IReadOnlyList<MissingDependency>? missing = null,
        IReadOnlyList<VersionConflict>? conflicts = null)
    {
        var parts = new List<string>();

        if (cycles is { Count: > 0 })
        {
            parts.Add($"检测到 {cycles.Count} 个循环依赖");
        }

        if (missing is { Count: > 0 })
        {
            parts.Add($"缺少 {missing.Count} 个必需依赖");
        }

        if (conflicts is { Count: > 0 })
        {
            parts.Add($"存在 {conflicts.Count} 个版本冲突");
        }

        var errorMessage = parts.Count > 0 ? string.Join("；", parts) : "依赖解析失败";

        return new DependencyResolveResult(false, Array.Empty<string>(), cycles, missing, conflicts, errorMessage);
    }

    #endregion
}

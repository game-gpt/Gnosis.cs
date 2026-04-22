namespace Gnosis.Plugin.Dependency;

/// <summary>
/// 依赖循环，描述插件间的循环依赖路径
/// </summary>
public sealed class DependencyCycle
{
    #region 属性

    /// <summary>
    /// 循环依赖路径
    /// </summary>
    public IReadOnlyList<string> Path { get; }

    /// <summary>
    /// 循环描述
    /// </summary>
    public string Description => string.Join(" → ", Path);

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用循环路径初始化
    /// </summary>
    /// <param name="path">循环依赖路径</param>
    public DependencyCycle(IReadOnlyList<string> path)
    {
        Path = path ?? Array.Empty<string>();
    }

    #endregion
}

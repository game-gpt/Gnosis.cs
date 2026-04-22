namespace Gnosis.Plugin.Isolation;

/// <summary>
/// 沙箱策略，定义沙箱的隔离行为规则
/// </summary>
public sealed class SandboxPolicy
{
    #region 属性

    /// <summary>
    /// 是否允许默认路径访问
    /// </summary>
    public bool AllowDefaultPathAccess { get; init; }

    /// <summary>
    /// 是否允许网络出站连接
    /// </summary>
    public bool AllowNetworkEgress { get; init; }

    /// <summary>
    /// 是否允许进程创建
    /// </summary>
    public bool AllowProcessSpawn { get; init; }

    /// <summary>
    /// 是否允许反射调用
    /// </summary>
    public bool AllowReflection { get; init; }

    /// <summary>
    /// 是否允许动态代码生成
    /// </summary>
    public bool AllowDynamicCodeGeneration { get; init; }

    /// <summary>
    /// 是否允许访问环境变量
    /// </summary>
    public bool AllowEnvironmentAccess { get; init; }

    /// <summary>
    /// 是否允许访问剪贴板
    /// </summary>
    public bool AllowClipboardAccess { get; init; }

    /// <summary>
    /// 最大递归深度
    /// </summary>
    public int MaxRecursionDepth { get; init; }

    /// <summary>
    /// 最大执行时间（毫秒），0 表示不限制
    /// </summary>
    public int MaxExecutionTimeMs { get; init; }

    #endregion

    #region 静态属性

    /// <summary>
    /// 默认沙箱策略
    /// </summary>
    public static SandboxPolicy Default => new()
    {
        AllowDefaultPathAccess = false,
        AllowNetworkEgress = false,
        AllowProcessSpawn = false,
        AllowReflection = false,
        AllowDynamicCodeGeneration = false,
        AllowEnvironmentAccess = false,
        AllowClipboardAccess = false,
        MaxRecursionDepth = 256,
        MaxExecutionTimeMs = 0
    };

    /// <summary>
    /// 宽松沙箱策略
    /// </summary>
    public static SandboxPolicy Permissive => new()
    {
        AllowDefaultPathAccess = true,
        AllowNetworkEgress = true,
        AllowProcessSpawn = false,
        AllowReflection = true,
        AllowDynamicCodeGeneration = false,
        AllowEnvironmentAccess = true,
        AllowClipboardAccess = true,
        MaxRecursionDepth = 1024,
        MaxExecutionTimeMs = 0
    };

    /// <summary>
    /// 严格沙箱策略
    /// </summary>
    public static SandboxPolicy Strict => new()
    {
        AllowDefaultPathAccess = false,
        AllowNetworkEgress = false,
        AllowProcessSpawn = false,
        AllowReflection = false,
        AllowDynamicCodeGeneration = false,
        AllowEnvironmentAccess = false,
        AllowClipboardAccess = false,
        MaxRecursionDepth = 64,
        MaxExecutionTimeMs = 5000
    };

    #endregion
}

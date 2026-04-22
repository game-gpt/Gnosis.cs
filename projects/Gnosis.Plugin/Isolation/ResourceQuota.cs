namespace Gnosis.Plugin.Isolation;

/// <summary>
/// 插件资源配额，限制插件的资源消耗
/// </summary>
public sealed class ResourceQuota
{
    #region 属性

    /// <summary>
    /// 最大内存使用量（字节），null 表示不限制
    /// </summary>
    public long? MemoryQuota { get; set; }

    /// <summary>
    /// 最大 CPU 时间占比（0.0 ~ 1.0），null 表示不限制
    /// </summary>
    public float? CpuQuota { get; set; }

    /// <summary>
    /// 最大文件句柄数，null 表示不限制
    /// </summary>
    public int? FileHandleQuota { get; set; }

    /// <summary>
    /// 最大网络连接数，null 表示不限制
    /// </summary>
    public int? NetworkConnectionQuota { get; set; }

    /// <summary>
    /// 最大线程数，null 表示不限制
    /// </summary>
    public int? ThreadQuota { get; set; }

    /// <summary>
    /// 每秒最大调用次数，null 表示不限制
    /// </summary>
    public int? CallRateLimit { get; set; }

    #endregion

    #region 公有方法

    /// <summary>
    /// 创建无限制的资源配额
    /// </summary>
    /// <returns>无限制的资源配额</returns>
    public static ResourceQuota Unlimited()
    {
        return new ResourceQuota();
    }

    /// <summary>
    /// 创建默认的资源配额
    /// </summary>
    /// <returns>默认的资源配额</returns>
    public static ResourceQuota Default()
    {
        return new ResourceQuota
        {
            MemoryQuota = 64 * 1024 * 1024,
            CpuQuota = 0.25f,
            FileHandleQuota = 32,
            NetworkConnectionQuota = 8,
            ThreadQuota = 4,
            CallRateLimit = 1000
        };
    }

    /// <summary>
    /// 创建严格的资源配额
    /// </summary>
    /// <returns>严格的资源配额</returns>
    public static ResourceQuota Strict()
    {
        return new ResourceQuota
        {
            MemoryQuota = 16 * 1024 * 1024,
            CpuQuota = 0.1f,
            FileHandleQuota = 8,
            NetworkConnectionQuota = 2,
            ThreadQuota = 1,
            CallRateLimit = 100
        };
    }

    #endregion
}

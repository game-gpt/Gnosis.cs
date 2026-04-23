namespace Gnosis.Graphic.RHI;

/// <summary>
///     RHI 资源句柄接口，仅供 RHI 实现层内部使用
///     不应暴露给上层调用者
/// </summary>
public interface IRhiResourceHandle
{
    /// <summary>
    ///     原生平台句柄
    /// </summary>
    nint NativeHandle { get; }
}

/// <summary>
///     RHI 资源标识接口，仅供 RHI 实现层内部使用
/// </summary>
public interface IRhiResourceKey
{
    /// <summary>
    ///     资源唯一标识
    /// </summary>
    ulong ResourceKey { get; }
}

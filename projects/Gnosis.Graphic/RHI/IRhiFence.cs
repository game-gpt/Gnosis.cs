namespace Gnosis.Graphic.RHI;

/// <summary>
/// 围栏接口，用于 CPU-GPU 同步
/// </summary>
public interface IRhiFence : IDisposable
{
    /// <summary>
    /// 围栏是否已触发
    /// </summary>
    bool IsSignaled { get; }

    /// <summary>
    /// 等待围栏触发
    /// </summary>
    /// <param name="timeout">超时时间（纳秒），ulong.MaxValue 表示无限等待</param>
    void Wait(ulong timeout = ulong.MaxValue);

    /// <summary>
    /// 重置围栏为未触发状态
    /// </summary>
    void Reset();
}

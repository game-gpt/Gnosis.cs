namespace Gnosis.XR.Input;

/// <summary>
/// XR 触觉反馈信息
/// </summary>
public readonly record struct XrHapticFeedback
{
    /// <summary>
    /// 振动幅度（0.0 ~ 1.0）
    /// </summary>
    public float Amplitude { get; init; }

    /// <summary>
    /// 振动持续时间（纳秒）
    /// </summary>
    public long DurationNanoseconds { get; init; }

    /// <summary>
    /// 振动频率（Hz，0 表示使用设备默认频率）
    /// </summary>
    public float Frequency { get; init; }

    /// <summary>
    /// 创建空触觉反馈（停止振动）
    /// </summary>
    public static XrHapticFeedback Stop => new()
    {
        Amplitude = 0.0f,
        DurationNanoseconds = 0,
        Frequency = 0.0f
    };

    /// <summary>
    /// 创建简单触觉反馈
    /// </summary>
    /// <param name="amplitude">振动幅度</param>
    /// <param name="durationMs">持续时间（毫秒）</param>
    /// <param name="frequency">振动频率（Hz）</param>
    /// <returns>触觉反馈信息</returns>
    public static XrHapticFeedback Simple(float amplitude, float durationMs, float frequency = 0.0f)
    {
        return new XrHapticFeedback
        {
            Amplitude = Math.Clamp(amplitude, 0.0f, 1.0f),
            DurationNanoseconds = (long)(durationMs * 1_000_000),
            Frequency = frequency
        };
    }
}

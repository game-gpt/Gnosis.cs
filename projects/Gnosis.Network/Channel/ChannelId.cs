namespace Gnosis.Network.Channel;

/// <summary>
/// 信道标识，用于唯一标识一个传输信道
/// </summary>
public readonly record struct ChannelId : IComparable<ChannelId>
{
    /// <summary>
    /// 空信道标识
    /// </summary>
    public static readonly ChannelId Empty = new(0);

    /// <summary>
    /// 默认信道标识（可靠有序）
    /// </summary>
    public static readonly ChannelId Default = new(0);

    /// <summary>
    /// 信道标识值
    /// </summary>
    public byte Value { get; }

    /// <summary>
    /// 初始化信道标识
    /// </summary>
    public ChannelId(byte value)
    {
        Value = value;
    }

    /// <summary>
    /// 比较信道标识
    /// </summary>
    public int CompareTo(ChannelId other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return $"Channel:{Value}";
    }
}

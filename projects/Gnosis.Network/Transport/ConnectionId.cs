namespace Gnosis.Network.Transport;

/// <summary>
/// 传输层连接标识，用于唯一标识一个网络连接
/// </summary>
public readonly record struct ConnectionId : IComparable<ConnectionId>
{
    /// <summary>
    /// 空连接标识
    /// </summary>
    public static readonly ConnectionId Empty = new(0);

    /// <summary>
    /// 连接标识值
    /// </summary>
    public ulong Value { get; }

    /// <summary>
    /// 初始化连接标识
    /// </summary>
    public ConnectionId(ulong value)
    {
        Value = value;
    }

    /// <summary>
    /// 比较连接标识
    /// </summary>
    public int CompareTo(ConnectionId other)
    {
        return Value.CompareTo(other.Value);
    }

    /// <summary>
    /// 生成新的唯一连接标识
    /// </summary>
    public static ConnectionId New()
    {
        return new ConnectionId((ulong)Random.Shared.NextInt64());
    }

    public override string ToString()
    {
        return $"Connection:{Value:X16}";
    }
}

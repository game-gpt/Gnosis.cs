using System.Collections.Generic;

namespace Gnosis.Network.Channel;

/// <summary>
/// 不可靠无序信道，不保证消息可靠性和有序性，丢弃过时消息
/// </summary>
public sealed class UnreliableChannel : IChannel
{
    #region 字段

    private ushort _sendSequence;
    private ushort _highestReceived;

    #endregion

    #region 属性

    /// <summary>
    /// 获取信道标识
    /// </summary>
    public ChannelId Id { get; }

    /// <summary>
    /// 获取信道类型
    /// </summary>
    public ChannelType ChannelType => ChannelType.UnreliableUnordered;

    /// <summary>
    /// 获取已发送的最新序列号
    /// </summary>
    public ushort SendSequence => _sendSequence;

    /// <summary>
    /// 获取已接收的最高序列号
    /// </summary>
    public ushort HighestReceived => _highestReceived;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化不可靠无序信道
    /// </summary>
    public UnreliableChannel(ChannelId id)
    {
        Id = id;
        _sendSequence = 0;
        _highestReceived = 0;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 发送数据，添加序列号头
    /// </summary>
    public void Send(ReadOnlySpan<byte> data)
    {
        _sendSequence++;
    }

    /// <summary>
    /// 处理接收到的原始数据，丢弃过时消息
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<byte>> ProcessIncoming(ReadOnlyMemory<byte> data)
    {
        var result = new List<ReadOnlyMemory<byte>>();

        if (data.Length < 2)
        {
            return result;
        }

        var sequence = ReadSequence(data.Span);
        var diff = (ushort)(sequence - _highestReceived);

        if (diff > 0 && diff < 32768)
        {
            _highestReceived = sequence;
        }
        else if (diff == 0)
        {
            return result;
        }
        else
        {
            return result;
        }

        var payload = ExtractPayload(data);
        result.Add(payload);

        return result;
    }

    /// <summary>
    /// 处理待发送的原始数据，添加序列号头
    /// </summary>
    public ReadOnlyMemory<byte> ProcessOutgoing(ReadOnlySpan<byte> data)
    {
        var sequence = _sendSequence;
        var framed = new byte[2 + data.Length];
        framed[0] = (byte)(sequence >> 8);
        framed[1] = (byte)(sequence & 0xFF);
        data.CopyTo(framed.AsSpan(2));
        return framed;
    }

    /// <summary>
    /// 更新信道状态（不可靠信道无需特殊更新）
    /// </summary>
    public void Update(TimeSpan deltaTime)
    {
    }

    /// <summary>
    /// 重置信道状态
    /// </summary>
    public void Reset()
    {
        _sendSequence = 0;
        _highestReceived = 0;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 从数据帧中读取序列号
    /// </summary>
    private static ushort ReadSequence(ReadOnlySpan<byte> data)
    {
        return (ushort)((data[0] << 8) | data[1]);
    }

    /// <summary>
    /// 从数据帧中提取载荷（跳过 2 字节序列号头）
    /// </summary>
    private static ReadOnlyMemory<byte> ExtractPayload(ReadOnlyMemory<byte> data)
    {
        return data[2..];
    }

    #endregion
}

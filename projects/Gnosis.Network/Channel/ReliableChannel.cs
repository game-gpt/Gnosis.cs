using System.Collections.Generic;

namespace Gnosis.Network.Channel;

/// <summary>
/// 可靠有序信道，保证消息可靠且按序到达
/// </summary>
public sealed class ReliableChannel : IChannel
{
    #region 字段

    private readonly ReliableChannelConfig _config;
    private readonly Dictionary<ushort, PendingMessage> _sendWindow = new();
    private readonly Dictionary<ushort, ReadOnlyMemory<byte>> _receiveBuffer = new();
    private readonly List<ReadOnlyMemory<byte>> _deliverable = new();
    private readonly HashSet<ushort> _receivedAcks = new();
    private readonly List<ushort> _pendingAckSequences = new();
    private ushort _sendSequence;
    private ushort _receiveSequence;
    private long _lastUpdateTime;

    #endregion

    #region 属性

    /// <summary>
    /// 获取信道标识
    /// </summary>
    public ChannelId Id { get; }

    /// <summary>
    /// 获取信道类型
    /// </summary>
    public ChannelType ChannelType => ChannelType.ReliableOrdered;

    /// <summary>
    /// 获取当前发送窗口中未确认的消息数量
    /// </summary>
    public int PendingCount => _sendWindow.Count;

    /// <summary>
    /// 获取接收缓冲区中的消息数量
    /// </summary>
    public int BufferedCount => _receiveBuffer.Count;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化可靠有序信道
    /// </summary>
    public ReliableChannel(ChannelId id, ReliableChannelConfig? config = null)
    {
        Id = id;
        _config = config ?? ReliableChannelConfig.Default;
        _sendSequence = 0;
        _receiveSequence = 0;
        _lastUpdateTime = 0;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 发送数据，添加序列号并缓存等待确认
    /// </summary>
    public void Send(ReadOnlySpan<byte> data)
    {
        if (data.Length > _config.MaxMessageSize)
        {
            throw new ArgumentException($"消息大小 {data.Length} 超过最大限制 {_config.MaxMessageSize}");
        }

        if (_sendWindow.Count >= _config.WindowSize)
        {
            throw new InvalidOperationException("发送窗口已满，无法发送更多消息");
        }

        var sequence = _sendSequence++;
        var framed = FrameOutgoing(sequence, data);
        _sendWindow[sequence] = new PendingMessage(framed.ToArray(), Environment.TickCount64);
    }

    /// <summary>
    /// 处理接收到的原始数据，提取序列号并按序投递
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<byte>> ProcessIncoming(ReadOnlyMemory<byte> data)
    {
        _deliverable.Clear();

        if (data.Length < 2)
        {
            return _deliverable;
        }

        var sequence = ReadSequence(data.Span);
        SendAck(sequence);

        var diff = (ushort)(sequence - _receiveSequence);

        if (diff == 0)
        {
            var payload = ExtractPayload(data);
            _deliverable.Add(payload);
            _receiveSequence++;

            DeliverBuffered();
        }
        else if (diff > 0 && diff < _config.WindowSize)
        {
            if (!_receiveBuffer.ContainsKey(sequence))
            {
                _receiveBuffer[sequence] = ExtractPayload(data);
            }
        }

        return _deliverable;
    }

    /// <summary>
    /// 处理待发送的原始数据，返回所有需要发送的帧（含重传）
    /// </summary>
    public ReadOnlyMemory<byte> ProcessOutgoing(ReadOnlySpan<byte> data)
    {
        var sequence = _sendSequence;
        return FrameOutgoing(sequence, data);
    }

    /// <summary>
    /// 更新信道状态，处理重传和 ACK 超时
    /// </summary>
    public void Update(TimeSpan deltaTime)
    {
        var now = Environment.TickCount64;
        _lastUpdateTime = now;

        ProcessReceivedAcks();
        RetransmitExpired(now);
    }

    /// <summary>
    /// 处理接收到的 ACK
    /// </summary>
    public void ProcessAck(ReadOnlySpan<byte> ackData)
    {
        if (ackData.Length < 2)
        {
            return;
        }

        var ackSequence = (ushort)((ackData[0] << 8) | ackData[1]);
        _receivedAcks.Add(ackSequence);
    }

    /// <summary>
    /// 获取需要发送的 ACK 数据
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<byte>> GetPendingAcks()
    {
        var acks = new List<ReadOnlyMemory<byte>>();

        foreach (var seq in _pendingAckSequences)
        {
            var ackBytes = new byte[2];
            ackBytes[0] = (byte)(seq >> 8);
            ackBytes[1] = (byte)(seq & 0xFF);
            acks.Add(ackBytes);
        }

        _pendingAckSequences.Clear();
        return acks;
    }

    /// <summary>
    /// 获取所有需要发送或重传的帧
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<byte>> GetPendingFrames()
    {
        var frames = new List<ReadOnlyMemory<byte>>();

        foreach (var (_, pending) in _sendWindow)
        {
            frames.Add(pending.Data);
        }

        return frames;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 处理已接收的 ACK，从发送窗口中移除已确认的消息
    /// </summary>
    private void ProcessReceivedAcks()
    {
        foreach (var ackSequence in _receivedAcks)
        {
            _sendWindow.Remove(ackSequence);
        }

        _receivedAcks.Clear();
    }

    /// <summary>
    /// 重传超时的消息
    /// </summary>
    private void RetransmitExpired(long now)
    {
        foreach (var (sequence, pending) in _sendWindow)
        {
            var elapsed = now - pending.Timestamp;

            if (elapsed > _config.RetransmitTimeoutMs)
            {
                if (pending.RetryCount >= _config.MaxRetries)
                {
                    continue;
                }

                pending.Timestamp = now;
                pending.RetryCount++;
            }
        }
    }

    /// <summary>
    /// 投递接收缓冲区中按序可用的消息
    /// </summary>
    private void DeliverBuffered()
    {
        while (_receiveBuffer.TryGetValue(_receiveSequence, out var buffered))
        {
            _deliverable.Add(buffered);
            _receiveBuffer.Remove(_receiveSequence);
            _receiveSequence++;
        }
    }

    /// <summary>
    /// 发送 ACK 确认
    /// </summary>
    private void SendAck(ushort sequence)
    {
        _pendingAckSequences.Add(sequence);
    }

    /// <summary>
    /// 封装出站数据帧，添加序列号头
    /// </summary>
    private static ReadOnlyMemory<byte> FrameOutgoing(ushort sequence, ReadOnlySpan<byte> data)
    {
        var framed = new byte[2 + data.Length];
        framed[0] = (byte)(sequence >> 8);
        framed[1] = (byte)(sequence & 0xFF);
        data.CopyTo(framed.AsSpan(2));
        return framed;
    }

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

    #region 内部类型

    /// <summary>
    /// 待确认消息
    /// </summary>
    private sealed class PendingMessage
    {
        public byte[] Data { get; }
        public long Timestamp { get; set; }
        public int RetryCount { get; set; }

        public PendingMessage(byte[] data, long timestamp)
        {
            Data = data;
            Timestamp = timestamp;
            RetryCount = 0;
        }
    }

    #endregion
}

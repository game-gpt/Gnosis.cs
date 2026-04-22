using Gnosis.Network.Channel;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network;

public class ChannelTests : GnosisTester
{
    #region ReliableChannel 测试

    [Test]
    public void ReliableChannel_初始化后属性正确()
    {
        var channel = new ReliableChannel(new ChannelId(1));

        Assert.That(channel.Id, Is.EqualTo(new ChannelId(1)));
        Assert.That(channel.ChannelType, Is.EqualTo(ChannelType.ReliableOrdered));
        Assert.That(channel.PendingCount, Is.EqualTo(0));
    }

    [Test]
    public void ReliableChannel_Send后待确认数增加()
    {
        var channel = new ReliableChannel(new ChannelId(1));
        channel.Send(new byte[] { 0x01, 0x02, 0x03 });

        Assert.That(channel.PendingCount, Is.EqualTo(1));
    }

    [Test]
    public void ReliableChannel_Send多次后待确认数增加()
    {
        var channel = new ReliableChannel(new ChannelId(1));
        channel.Send(new byte[] { 0x01 });
        channel.Send(new byte[] { 0x02 });
        channel.Send(new byte[] { 0x03 });

        Assert.That(channel.PendingCount, Is.EqualTo(3));
    }

    [Test]
    public void ReliableChannel_ProcessIncoming_按序接收消息()
    {
        var sender = new ReliableChannel(new ChannelId(1));
        var receiver = new ReliableChannel(new ChannelId(1));

        sender.Send(new byte[] { 0x01, 0x02, 0x03 });
        var outgoing = sender.ProcessOutgoing();

        foreach (var packet in outgoing)
        {
            receiver.ProcessIncoming(packet.Span);
        }

        var delivered = receiver.DeliverableMessages();
        Assert.That(delivered.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReliableChannel_ProcessIncoming_乱序接收消息按序投递()
    {
        var sender = new ReliableChannel(new ChannelId(1));
        var receiver = new ReliableChannel(new ChannelId(1));

        sender.Send(new byte[] { 0x01 });
        sender.Send(new byte[] { 0x02 });
        sender.Send(new byte[] { 0x03 });

        var outgoing = sender.ProcessOutgoing();

        receiver.ProcessIncoming(outgoing[2].Span);
        receiver.ProcessIncoming(outgoing[0].Span);
        receiver.ProcessIncoming(outgoing[1].Span);

        var delivered = receiver.DeliverableMessages();
        Assert.That(delivered.Count, Is.EqualTo(3));
    }

    [Test]
    public void ReliableChannel_重复消息不重复投递()
    {
        var sender = new ReliableChannel(new ChannelId(1));
        var receiver = new ReliableChannel(new ChannelId(1));

        sender.Send(new byte[] { 0x01 });
        var outgoing = sender.ProcessOutgoing();

        receiver.ProcessIncoming(outgoing[0].Span);
        receiver.ProcessIncoming(outgoing[0].Span);

        var delivered = receiver.DeliverableMessages();
        Assert.That(delivered.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReliableChannel_自定义配置()
    {
        var config = new ReliableChannelConfig
        {
            RetransmitTimeoutMs = 100,
            MaxRetries = 5,
            WindowSize = 16
        };
        var channel = new ReliableChannel(new ChannelId(1), config);

        Assert.That(channel.Id, Is.EqualTo(new ChannelId(1)));
    }

    #endregion

    #region UnreliableChannel 测试

    [Test]
    public void UnreliableChannel_初始化后属性正确()
    {
        var channel = new UnreliableChannel(new ChannelId(2));

        Assert.That(channel.Id, Is.EqualTo(new ChannelId(2)));
        Assert.That(channel.ChannelType, Is.EqualTo(ChannelType.UnreliableUnordered));
        Assert.That(channel.SendSequence, Is.EqualTo(0));
    }

    [Test]
    public void UnreliableChannel_Send后序列号递增()
    {
        var channel = new UnreliableChannel(new ChannelId(2));
        channel.Send(new byte[] { 0x01 });
        channel.Send(new byte[] { 0x02 });

        Assert.That(channel.SendSequence, Is.EqualTo(2));
    }

    [Test]
    public void UnreliableChannel_ProcessIncoming_新消息可投递()
    {
        var sender = new UnreliableChannel(new ChannelId(2));
        var receiver = new UnreliableChannel(new ChannelId(2));

        sender.Send(new byte[] { 0x01, 0x02 });
        var outgoing = sender.ProcessOutgoing();

        foreach (var packet in outgoing)
        {
            receiver.ProcessIncoming(packet.Span);
        }

        var delivered = receiver.DeliverableMessages();
        Assert.That(delivered.Count, Is.EqualTo(1));
    }

    [Test]
    public void UnreliableChannel_ProcessIncoming_过时消息被丢弃()
    {
        var sender = new UnreliableChannel(new ChannelId(2));
        var receiver = new UnreliableChannel(new ChannelId(2));

        sender.Send(new byte[] { 0x01 });
        sender.Send(new byte[] { 0x02 });
        var outgoing = sender.ProcessOutgoing();

        receiver.ProcessIncoming(outgoing[1].Span);
        receiver.ProcessIncoming(outgoing[0].Span);

        var delivered = receiver.DeliverableMessages();
        Assert.That(delivered.Count, Is.EqualTo(1));
    }

    [Test]
    public void UnreliableChannel_HighestReceived_正确更新()
    {
        var sender = new UnreliableChannel(new ChannelId(2));
        var receiver = new UnreliableChannel(new ChannelId(2));

        sender.Send(new byte[] { 0x01 });
        sender.Send(new byte[] { 0x02 });
        sender.Send(new byte[] { 0x03 });
        var outgoing = sender.ProcessOutgoing();

        receiver.ProcessIncoming(outgoing[2].Span);

        Assert.That(receiver.HighestReceived, Is.EqualTo(3));
    }

    #endregion

    #region ChannelId 测试

    [Test]
    public void ChannelId_Equality_相同值相等()
    {
        var id1 = new ChannelId(5);
        var id2 = new ChannelId(5);

        Assert.That(id1, Is.EqualTo(id2));
        Assert.That(id1.GetHashCode(), Is.EqualTo(id2.GetHashCode()));
    }

    [Test]
    public void ChannelId_Comparison_可排序()
    {
        var id1 = new ChannelId(1);
        var id2 = new ChannelId(2);

        Assert.That(id1.CompareTo(id2), Is.LessThan(0));
        Assert.That(id2.CompareTo(id1), Is.GreaterThan(0));
    }

    [Test]
    public void ChannelId_Default_值为零()
    {
        Assert.That(ChannelId.Default.Value, Is.EqualTo(0));
    }

    [Test]
    public void ChannelId_Empty_值为零()
    {
        Assert.That(ChannelId.Empty.Value, Is.EqualTo(0));
    }

    #endregion

    #region ChannelType 测试

    [Test]
    public void ChannelType_枚举值正确()
    {
        Assert.That((byte)ChannelType.ReliableOrdered, Is.EqualTo(0));
        Assert.That((byte)ChannelType.ReliableUnordered, Is.EqualTo(1));
        Assert.That((byte)ChannelType.UnreliableOrdered, Is.EqualTo(2));
        Assert.That((byte)ChannelType.UnreliableUnordered, Is.EqualTo(3));
    }

    #endregion
}

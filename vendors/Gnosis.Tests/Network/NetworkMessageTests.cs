using Gnosis.Core;
using Gnosis.Infrastructure;
using Gnosis.Network.Serialization;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Network;

public class NetworkMessageTests : GnosisTester
{
    [Test]
    public void Create_设置所有字段()
    {
        var senderId = PlayerId.New();
        var payload = new byte[] { 1, 2, 3 };

        var message = NetworkMessage.Create(42, senderId, payload, true);

        Assert.That(message.MessageId, Is.EqualTo(42));
        Assert.That(message.SenderId, Is.EqualTo(senderId));
        Assert.That(message.IsReliable, Is.True);
    }

    [Test]
    public void Create_自动设置时间戳()
    {
        var before = Timestamp.Now;
        var message = NetworkMessage.Create(1, PlayerId.New(), []);
        var after = Timestamp.Now;

        Assert.That(message.Timestamp, Is.GreaterThanOrEqualTo(before));
        Assert.That(message.Timestamp, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void Create_默认不可靠()
    {
        var message = NetworkMessage.Create(1, PlayerId.New(), []);

        Assert.That(message.IsReliable, Is.False);
    }

    [Test]
    public void Payload_返回正确视图()
    {
        var payload = new byte[] { 10, 20, 30 };
        var message = NetworkMessage.Create(1, PlayerId.New(), payload);

        var span = message.Payload;

        Assert.That(span.Length, Is.EqualTo(3));
        Assert.That(span[0], Is.EqualTo(10));
        Assert.That(span[1], Is.EqualTo(20));
        Assert.That(span[2], Is.EqualTo(30));
    }

    [Test]
    public void Payload_空载荷返回空视图()
    {
        var message = NetworkMessage.Create(1, PlayerId.New(), []);

        Assert.That(message.Payload.IsEmpty, Is.True);
    }
}
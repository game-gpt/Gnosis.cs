using Gnosis.Network.Serialization;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Network;

public class MessageSerializerTests : GnosisTester
{
    private MessageSerializer _serializer = null!;

    public override void Setup()
    {
        base.Setup();
        _serializer = new MessageSerializer();
    }

    [Test]
    public void Serialize_然后Deserialize_往返一致()
    {
        var original = new TestStruct { X = 42, Y = 3.14f };

        var bytes = _serializer.Serialize(original);
        var restored = _serializer.Deserialize<TestStruct>(bytes);

        Assert.That(restored.X, Is.EqualTo(original.X));
        Assert.That(restored.Y, Is.EqualTo(original.Y));
    }

    [Test]
    public void Deserialize_空数据抛出ArgumentException()
    {
        AssertThrows<ArgumentException>(() => _serializer.Deserialize<TestStruct>(ReadOnlySpan<byte>.Empty), "反序列化数据不能为空");
    }

    [Test]
    public void Deserialize_数据不足抛出ArgumentException()
    {
        var data = new byte[1];

        AssertThrows<ArgumentException>(() => _serializer.Deserialize<TestStruct>(data), "反序列化数据长度不足");
    }

    [Test]
    public void Serialize_返回非空字节数组()
    {
        var original = new TestStruct { X = 1, Y = 2.0f };

        var bytes = _serializer.Serialize(original);

        Assert.That(bytes, Is.Not.Null);
        Assert.That(bytes.Length, Is.GreaterThan(0));
    }

    private struct TestStruct
    {
        public int X;
        public float Y;
    }
}
using Gnosis.Network.Serialization;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network;

public class BitStreamTests : GnosisTester
{
    [Test]
    public void WriteBit_True值写入后可正确读取()
    {
        var writer = new BitStream(4);
        writer.WriteBit(true);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadBit(), Is.True);
    }

    [Test]
    public void WriteBit_False值写入后可正确读取()
    {
        var writer = new BitStream(4);
        writer.WriteBit(false);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadBit(), Is.False);
    }

    [Test]
    public void WriteByte_写入后可正确读取()
    {
        var writer = new BitStream(4);
        writer.WriteByte(0xAB);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadByte(), Is.EqualTo(0xAB));
    }

    [Test]
    public void WriteUInt16_写入后可正确读取()
    {
        var writer = new BitStream(4);
        writer.WriteUInt16(0x1234);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadUInt16(), Is.EqualTo(0x1234));
    }

    [Test]
    public void WriteUInt32_写入后可正确读取()
    {
        var writer = new BitStream(8);
        writer.WriteUInt32(0xDEADBEEF);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadUInt32(), Is.EqualTo(0xDEADBEEFu));
    }

    [Test]
    public void WriteInt32_正数写入后可正确读取()
    {
        var writer = new BitStream(8);
        writer.WriteInt32(12345);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadInt32(), Is.EqualTo(12345));
    }

    [Test]
    public void WriteInt32_负数写入后可正确读取()
    {
        var writer = new BitStream(8);
        writer.WriteInt32(-98765);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadInt32(), Is.EqualTo(-98765));
    }

    [Test]
    public void WriteFloat_写入后可正确读取()
    {
        var writer = new BitStream(8);
        writer.WriteFloat(3.14f);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadFloat(), Is.EqualTo(3.14f));
    }

    [Test]
    public void WriteDouble_写入后可正确读取()
    {
        var writer = new BitStream(16);
        writer.WriteDouble(2.718281828);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadDouble(), Is.EqualTo(2.718281828));
    }

    [Test]
    public void WriteBool_写入后可正确读取()
    {
        var writer = new BitStream(4);
        writer.WriteBool(true);
        writer.WriteBool(false);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadBool(), Is.True);
        Assert.That(reader.ReadBool(), Is.False);
    }

    [Test]
    public void WriteRangedInt_范围内值写入后可正确读取()
    {
        var writer = new BitStream(8);
        writer.WriteRangedInt(50, 0, 100);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadRangedInt(0, 100), Is.EqualTo(50));
    }

    [Test]
    public void WriteRangedFloat_范围内值写入后可近似读取()
    {
        var writer = new BitStream(8);
        writer.WriteRangedFloat(0.5f, 0f, 1f, 8);

        var reader = new BitStream(writer.Buffer);
        var value = reader.ReadRangedFloat(0f, 1f, 8);
        Assert.That(value, Is.InRange(0.48f, 0.52f));
    }

    [Test]
    public void WriteString_写入后可正确读取()
    {
        var writer = new BitStream(64);
        writer.WriteString("你好，Gnosis！");

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadString(), Is.EqualTo("你好，Gnosis！"));
    }

    [Test]
    public void WriteBytes_写入后可正确读取()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var writer = new BitStream(16);
        writer.WriteBytes(data);

        var reader = new BitStream(writer.Buffer);
        var read = reader.ReadBytes();
        AssertCollectionsEqual(data, read);
    }

    [Test]
    public void WriteBits_指定比特数写入后可正确读取()
    {
        var writer = new BitStream(4);
        writer.WriteBits(0b101, 3);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadBits(3), Is.EqualTo(0b101u));
    }

    [Test]
    public void WriteBits_比特数超出范围抛出异常()
    {
        var writer = new BitStream(4);
        AssertThrows<ArgumentOutOfRangeException>(() => writer.WriteBits(0, 0));
        AssertThrows<ArgumentOutOfRangeException>(() => writer.WriteBits(0, 33));
    }

    [Test]
    public void ReadBits_超出剩余比特数抛出异常()
    {
        var writer = new BitStream(4);
        writer.WriteBits(0xFF, 8);

        var reader = new BitStream(writer.Buffer);
        AssertThrows<InvalidOperationException>(() => reader.ReadBits(16));
    }

    [Test]
    public void BitsRequired_零返回一()
    {
        Assert.That(BitStream.BitsRequired(0), Is.EqualTo(1));
    }

    [Test]
    public void BitsRequired_255返回8()
    {
        Assert.That(BitStream.BitsRequired(255), Is.EqualTo(8));
    }

    [Test]
    public void BitsRequired_256返回9()
    {
        Assert.That(BitStream.BitsRequired(256), Is.EqualTo(9));
    }

    [Test]
    public void BitLength_写入后正确更新()
    {
        var writer = new BitStream(4);
        Assert.That(writer.BitLength, Is.EqualTo(0));

        writer.WriteBits(0xFF, 8);
        Assert.That(writer.BitLength, Is.EqualTo(8));

        writer.WriteBit(true);
        Assert.That(writer.BitLength, Is.EqualTo(9));
    }

    [Test]
    public void RemainingBits_写入后正确计算()
    {
        var writer = new BitStream(4);
        writer.WriteUInt32(0);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.RemainingBits, Is.EqualTo(32));

        reader.ReadBits(16);
        Assert.That(reader.RemainingBits, Is.EqualTo(16));
    }

    [Test]
    public void AlignToByte_对齐后位置为字节边界()
    {
        var writer = new BitStream(4);
        writer.WriteBits(0b101, 3);
        Assert.That(writer.BitPosition, Is.EqualTo(3));

        writer.AlignToByte();
        Assert.That(writer.BitPosition, Is.EqualTo(8));
    }

    [Test]
    public void Reset_重置后可重新读取()
    {
        var writer = new BitStream(4);
        writer.WriteByte(0x42);

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadByte(), Is.EqualTo(0x42));

        reader.Reset();
        Assert.That(reader.ReadByte(), Is.EqualTo(0x42));
    }

    [Test]
    public void 混合类型读写_所有值正确还原()
    {
        var writer = new BitStream(128);
        writer.WriteBool(true);
        writer.WriteByte(0xAB);
        writer.WriteInt32(-42);
        writer.WriteFloat(1.5f);
        writer.WriteString("test");

        var reader = new BitStream(writer.Buffer);
        Assert.That(reader.ReadBool(), Is.True);
        Assert.That(reader.ReadByte(), Is.EqualTo(0xAB));
        Assert.That(reader.ReadInt32(), Is.EqualTo(-42));
        Assert.That(reader.ReadFloat(), Is.EqualTo(1.5f));
        Assert.That(reader.ReadString(), Is.EqualTo("test"));
    }
}

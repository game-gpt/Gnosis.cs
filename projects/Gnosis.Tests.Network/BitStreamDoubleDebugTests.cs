using System;
using Gnosis.Network.Serialization;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network;

public class BitStreamDoubleDebugTests : GnosisTester
{
    [Test]
    public void WriteDouble_调试()
    {
        var writer = new BitStream(16);
        var originalValue = 2.718281828;
        var originalBits = BitConverter.DoubleToUInt64Bits(originalValue);
        var originalBytes = BitConverter.GetBytes(originalValue);

        writer.WriteDouble(originalValue);

        var buffer = writer.Buffer;
        var writtenBytes = buffer.ToArray();

        Console.WriteLine($"Original value: {originalValue}");
        Console.WriteLine($"Original bits: {originalBits:X16}");
        Console.WriteLine($"Original bytes: {BitConverter.ToString(originalBytes)}");
        Console.WriteLine($"Written bytes: {BitConverter.ToString(writtenBytes)}");

        var reader = new BitStream(buffer);
        var readBits = reader.ReadUInt64();
        var readValue = BitConverter.UInt64BitsToDouble(readBits);

        Console.WriteLine($"Read bits: {readBits:X16}");
        Console.WriteLine($"Read value: {readValue}");

        Assert.That(readValue, Is.EqualTo(originalValue));
    }

    [Test]
    public void WriteUInt64_调试()
    {
        var writer = new BitStream(16);
        var originalBits = 0x4005BF0A8B145769UL;

        writer.WriteUInt64(originalBits);

        var buffer = writer.Buffer;
        var reader = new BitStream(buffer);
        var readBits = reader.ReadUInt64();

        Console.WriteLine($"Original bits: {originalBits:X16}");
        Console.WriteLine($"Read bits: {readBits:X16}");

        Assert.That(readBits, Is.EqualTo(originalBits));
    }
}

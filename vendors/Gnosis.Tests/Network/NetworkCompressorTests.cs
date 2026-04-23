using Gnosis.Network.Serialization;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network;

public class NetworkCompressorTests : GnosisTester
{
    [Test]
    public void Compress_空数据返回None算法头()
    {
        var data = Array.Empty<byte>();
        var compressed = NetworkCompressor.Compress(data, CompressionAlgorithm.Rle);

        Assert.That(compressed[0], Is.EqualTo((byte)CompressionAlgorithm.None));
    }

    [Test]
    public void Compress_RLE压缩重复数据后可正确解压()
    {
        var data = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xBB, 0xBB, 0xCC };
        var compressed = NetworkCompressor.Compress(data, CompressionAlgorithm.Rle);
        var decompressed = NetworkCompressor.Decompress(compressed);

        AssertCollectionsEqual(data, decompressed);
    }

    [Test]
    public void Compress_RLE压缩全零数据后可正确解压()
    {
        var data = new byte[256];
        var compressed = NetworkCompressor.Compress(data, CompressionAlgorithm.Rle);
        var decompressed = NetworkCompressor.Decompress(compressed);

        AssertCollectionsEqual(data, decompressed);
    }

    [Test]
    public void Compress_RLE压缩全相同字节后可正确解压()
    {
        var data = Enumerable.Repeat((byte)0xFF, 100).ToArray();
        var compressed = NetworkCompressor.Compress(data, CompressionAlgorithm.Rle);
        var decompressed = NetworkCompressor.Decompress(compressed);

        AssertCollectionsEqual(data, decompressed);
    }

    [Test]
    public void Compress_LzLite压缩重复模式数据后可正确解压()
    {
        var pattern = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var data = new byte[64];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = pattern[i % pattern.Length];
        }

        var compressed = NetworkCompressor.Compress(data, CompressionAlgorithm.LzLite);
        var decompressed = NetworkCompressor.Decompress(compressed);

        AssertCollectionsEqual(data, decompressed);
    }

    [Test]
    public void Compress_LzLite压缩包含0xFF的数据后可正确解压()
    {
        var data = new byte[] { 0x01, 0xFF, 0x02, 0xFF, 0xFF, 0x03 };
        var compressed = NetworkCompressor.Compress(data, CompressionAlgorithm.LzLite);
        var decompressed = NetworkCompressor.Decompress(compressed);

        AssertCollectionsEqual(data, decompressed);
    }

    [Test]
    public void Compress_None算法不压缩数据()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var compressed = NetworkCompressor.Compress(data, CompressionAlgorithm.None);
        var decompressed = NetworkCompressor.Decompress(compressed);

        AssertCollectionsEqual(data, decompressed);
    }

    [Test]
    public void Compress_压缩后数据更大时自动回退None()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        var compressed = NetworkCompressor.Compress(data, CompressionAlgorithm.Rle);

        Assert.That(compressed[0], Is.EqualTo((byte)CompressionAlgorithm.None));
    }

    [Test]
    public void Decompress_无效数据长度抛出异常()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };
        AssertThrows<ArgumentException>(() => NetworkCompressor.Decompress(data));
    }

    [Test]
    public void SelectAlgorithm_小数据返回None()
    {
        var data = new byte[8];
        Assert.That(NetworkCompressor.SelectAlgorithm(data), Is.EqualTo(CompressionAlgorithm.None));
    }

    [Test]
    public void SelectAlgorithm_高度重复数据返回Rle()
    {
        var data = new byte[256];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(i / 64);
        }

        var algorithm = NetworkCompressor.SelectAlgorithm(data);
        Assert.That(algorithm, Is.EqualTo(CompressionAlgorithm.Rle));
    }

    [Test]
    public void Compress_随机数据压缩解压后一致()
    {
        var random = new Random(42);
        var data = new byte[128];
        random.NextBytes(data);

        var compressed = NetworkCompressor.Compress(data, CompressionAlgorithm.LzLite);
        var decompressed = NetworkCompressor.Decompress(compressed);

        AssertCollectionsEqual(data, decompressed);
    }

    [Test]
    public void Compress_单字节数据压缩解压后一致()
    {
        var data = new byte[] { 0x42 };
        var compressed = NetworkCompressor.Compress(data, CompressionAlgorithm.Rle);
        var decompressed = NetworkCompressor.Decompress(compressed);

        AssertCollectionsEqual(data, decompressed);
    }
}

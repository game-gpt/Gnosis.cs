using System.IO.Compression;
using K4os.Compression.LZ4;
using ZstdNet;

namespace Gnosis.Assets.Formats.Compression;

public sealed class CompressionService
{
    #region Public Methods

    public static byte[] Compress(byte[] data, CompressionType compressionType)
    {
        if (compressionType == CompressionType.None)
        {
            return data;
        }

        return compressionType switch
        {
            CompressionType.LZ4 => CompressLz4(data),
            CompressionType.Zstd => CompressZstd(data),
            CompressionType.Gzip => CompressGzip(data),
            CompressionType.Brotli => CompressBrotli(data),
            CompressionType.Oodle => throw new NotSupportedException("Oodle 压缩暂不支持"),
            _ => throw new ArgumentOutOfRangeException(nameof(compressionType), $"未知的压缩类型：{compressionType}")
        };
    }

    public static byte[] Decompress(byte[] data, CompressionType compressionType)
    {
        if (compressionType == CompressionType.None)
        {
            return data;
        }

        return compressionType switch
        {
            CompressionType.LZ4 => DecompressLz4(data),
            CompressionType.Zstd => DecompressZstd(data),
            CompressionType.Gzip => DecompressGzip(data),
            CompressionType.Brotli => DecompressBrotli(data),
            CompressionType.Oodle => throw new NotSupportedException("Oodle 解压暂不支持"),
            _ => throw new ArgumentOutOfRangeException(nameof(compressionType), $"未知的压缩类型：{compressionType}")
        };
    }

    #endregion

    #region Private Methods

    private static byte[] CompressLz4(byte[] data)
    {
        return LZ4Pickler.Pickle(data);
    }

    private static byte[] DecompressLz4(byte[] data)
    {
        return LZ4Pickler.Unpickle(data);
    }

    private static byte[] CompressZstd(byte[] data)
    {
        using var compressor = new Compressor();
        return compressor.Wrap(data);
    }

    private static byte[] DecompressZstd(byte[] data)
    {
        using var decompressor = new Decompressor();
        return decompressor.Unwrap(data);
    }

    private static byte[] CompressGzip(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] DecompressGzip(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] CompressBrotli(byte[] data)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.Optimal))
        {
            brotli.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] DecompressBrotli(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }

    #endregion
}

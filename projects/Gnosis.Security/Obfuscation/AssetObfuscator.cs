using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Obfuscation;

public sealed class AssetObfuscator
{
    #region 字段

    private readonly Random _random = new();
    private int _headerSize = 64;
    private int _blockSize = 4096;

    #endregion

    #region 属性

    public bool IsHeaderObfuscationEnabled { get; private set; }
    public bool IsBlockShuffleEnabled { get; private set; }

    #endregion

    #region 公开方法

    public void EnableHeaderObfuscation(int headerSize = 64)
    {
        IsHeaderObfuscationEnabled = true;
        _headerSize = headerSize;
    }

    public void EnableBlockShuffle(int blockSize = 4096)
    {
        IsBlockShuffleEnabled = true;
        _blockSize = blockSize;
    }

    public byte[] Obfuscate(byte[] data)
    {
        if (data is null || data.Length == 0) throw new SecurityException("混淆数据不能为空");

        var result = CopyBytes(data);

        if (IsHeaderObfuscationEnabled)
        {
            result = ObfuscateHeader(result);
        }

        if (IsBlockShuffleEnabled)
        {
            result = ShuffleBlocks(result);
        }

        return result;
    }

    public byte[] Deobfuscate(byte[] obfuscatedData)
    {
        if (obfuscatedData is null || obfuscatedData.Length == 0) throw new SecurityException("还原数据不能为空");

        var result = CopyBytes(obfuscatedData);

        if (IsBlockShuffleEnabled)
        {
            result = UnshuffleBlocks(result);
        }

        if (IsHeaderObfuscationEnabled)
        {
            result = DeobfuscateHeader(result);
        }

        return result;
    }

    #endregion

    #region 私有方法

    private byte[] ObfuscateHeader(byte[] data)
    {
        if (data.Length < _headerSize) return data;

        var result = CopyBytes(data);
        var key = (byte)_random.Next(1, 256);

        for (var i = 0; i < _headerSize && i < result.Length; i++)
        {
            result[i] = (byte)(result[i] ^ key);
        }

        result[0] = (byte)(result[0] ^ key);

        return result;
    }

    private byte[] DeobfuscateHeader(byte[] data)
    {
        if (data.Length < _headerSize) return data;

        var key = data[0];

        for (var i = 0; i < _headerSize && i < data.Length; i++)
        {
            data[i] = (byte)(data[i] ^ key);
        }

        return data;
    }

    private byte[] ShuffleBlocks(byte[] data)
    {
        if (data.Length < _blockSize * 2) return data;

        var headerSize = _headerSize;
        var bodyData = new byte[data.Length - headerSize];
        Buffer.BlockCopy(data, headerSize, bodyData, 0, bodyData.Length);

        var blockCount = (bodyData.Length + _blockSize - 1) / _blockSize;
        var permutation = GeneratePermutation(blockCount);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(data, 0, headerSize);
        writer.Write(blockCount);

        foreach (var index in permutation)
        {
            var start = index * _blockSize;
            var length = Math.Min(_blockSize, bodyData.Length - start);
            writer.Write(index);
            writer.Write(length);
            writer.Write(bodyData, start, length);
        }

        return stream.ToArray();
    }

    private byte[] UnshuffleBlocks(byte[] data)
    {
        if (data.Length < _headerSize + 4) return data;

        var headerSize = _headerSize;

        using var bodyStream = new MemoryStream(data, headerSize, data.Length - headerSize);
        using var reader = new BinaryReader(bodyStream);

        var blockCount = reader.ReadInt32();
        var blocks = new (int Index, byte[] Data)[blockCount];

        for (var i = 0; i < blockCount; i++)
        {
            var originalIndex = reader.ReadInt32();
            var length = reader.ReadInt32();
            var blockData = reader.ReadBytes(length);
            blocks[i] = (originalIndex, blockData);
        }

        using var resultStream = new MemoryStream();
        using var resultWriter = new BinaryWriter(resultStream);

        resultWriter.Write(data, 0, headerSize);

        foreach (var block in blocks.OrderBy(b => b.Index))
        {
            resultWriter.Write(block.Data);
        }

        return resultStream.ToArray();
    }

    private int[] GeneratePermutation(int count)
    {
        var permutation = new int[count];

        for (var i = 0; i < count; i++)
        {
            permutation[i] = i;
        }

        for (var i = count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
        }

        return permutation;
    }

    private static byte[] CopyBytes(byte[] source)
    {
        var copy = new byte[source.Length];
        Buffer.BlockCopy(source, 0, copy, 0, source.Length);
        return copy;
    }

    #endregion
}

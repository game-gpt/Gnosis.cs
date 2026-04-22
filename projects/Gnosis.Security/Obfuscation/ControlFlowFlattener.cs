using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Obfuscation;

public sealed class ControlFlowFlattener
{
    #region 常量

    private const int BlockHeaderSize = 4;

    #endregion

    #region 字段

    private readonly Random _random = new();

    #endregion

    #region 属性

    public bool IsBogusControlFlowEnabled { get; private set; }
    public bool IsOpaquePredicateEnabled { get; private set; }

    #endregion

    #region 公开方法

    public void EnableBogusControlFlow() => IsBogusControlFlowEnabled = true;

    public void EnableOpaquePredicate() => IsOpaquePredicateEnabled = true;

    public byte[] Flatten(byte[] bytecode)
    {
        if (bytecode is null || bytecode.Length == 0) throw new SecurityException("字节码不能为空");
        if (bytecode.Length < BlockHeaderSize) return bytecode;

        var blocks = SplitBlocks(bytecode);
        ShuffleBlocks(blocks);

        return ReassembleWithDispatcher(blocks);
    }

    #endregion

    #region 私有方法

    private List<Block> SplitBlocks(byte[] bytecode)
    {
        var blocks = new List<Block>();
        var offset = 0;

        while (offset < bytecode.Length)
        {
            var remaining = bytecode.Length - offset;
            var blockSize = Math.Min(remaining, _random.Next(8, 33));
            var blockData = new byte[blockSize];
            Buffer.BlockCopy(bytecode, offset, blockData, 0, blockSize);

            blocks.Add(new Block(blocks.Count, blockData));
            offset += blockSize;
        }

        return blocks;
    }

    private void ShuffleBlocks(List<Block> blocks)
    {
        var n = blocks.Count;

        for (var i = n - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (blocks[i], blocks[j]) = (blocks[j], blocks[i]);
        }
    }

    private byte[] ReassembleWithDispatcher(List<Block> blocks)
    {
        var stateVar = (uint)_random.Next(1, int.MaxValue);
        var stateMap = new Dictionary<int, uint>();

        foreach (var block in blocks)
        {
            var state = (uint)_random.Next(1, int.MaxValue);
            stateMap[block.OriginalIndex] = state;
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(stateVar);

        foreach (var block in blocks)
        {
            writer.Write(stateMap[block.OriginalIndex]);
            writer.Write(block.Data.Length);
            writer.Write(block.Data);

            if (IsBogusControlFlowEnabled)
            {
                WriteBogusBlock(writer);
            }
        }

        if (IsOpaquePredicateEnabled)
        {
            writer.Write(GenerateOpaquePredicate());
        }

        writer.Write(0u);

        return stream.ToArray();
    }

    private void WriteBogusBlock(BinaryWriter writer)
    {
        var bogusSize = _random.Next(4, 17);
        var bogusData = new byte[bogusSize];
        _random.NextBytes(bogusData);

        writer.Write(0xDEADu);
        writer.Write(bogusSize);
        writer.Write(bogusData);
    }

    private byte[] GenerateOpaquePredicate()
    {
        var predicate = new byte[8];
        _random.NextBytes(predicate);

        var a = BitConverter.ToUInt32(predicate, 0);
        var b = BitConverter.ToUInt32(predicate, 4);
        var result = a ^ b ^ a;

        return BitConverter.GetBytes(result);
    }

    #endregion

    private sealed class Block(int originalIndex, byte[] data)
    {
        public int OriginalIndex { get; } = originalIndex;
        public byte[] Data { get; } = data;
    }
}

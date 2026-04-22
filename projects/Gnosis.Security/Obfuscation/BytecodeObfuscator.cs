using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Obfuscation;

public sealed class BytecodeObfuscator
{
    #region 字段

    private readonly Random _random = new();

    #endregion

    #region 属性

    public bool IsIsaRandomizationEnabled { get; private set; }
    public bool IsControlFlowFlatteningEnabled { get; private set; }
    public bool IsStringEncryptionEnabled { get; private set; }

    #endregion

    #region 公开方法

    public void EnableIsaRandomization() => IsIsaRandomizationEnabled = true;

    public void EnableControlFlowFlattening() => IsControlFlowFlatteningEnabled = true;

    public void EnableStringEncryption() => IsStringEncryptionEnabled = true;

    public byte[] Obfuscate(byte[] bytecode)
    {
        if (bytecode is null || bytecode.Length == 0) throw new SecurityException("混淆字节码不能为空");

        var result = bytecode;

        if (IsIsaRandomizationEnabled)
        {
            result = ApplyIsaRandomization(result);
        }

        if (IsControlFlowFlatteningEnabled)
        {
            result = ApplyControlFlowObfuscation(result);
        }

        if (IsStringEncryptionEnabled)
        {
            result = ApplyStringObfuscation(result);
        }

        return result;
    }

    public int GenerateIsaSeed() => _random.Next();

    #endregion

    #region 私有方法

    private byte[] ApplyIsaRandomization(byte[] bytecode)
    {
        var seed = GenerateIsaSeed();
        var substitutionTable = BuildSubstitutionTable(seed);

        var result = new byte[bytecode.Length + 8];
        Buffer.BlockCopy(BitConverter.GetBytes(seed), 0, result, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(bytecode.Length), 0, result, 4, 4);

        for (var i = 0; i < bytecode.Length; i++)
        {
            result[i + 8] = substitutionTable[bytecode[i]];
        }

        return result;
    }

    private byte[] ApplyControlFlowObfuscation(byte[] bytecode)
    {
        var junkInsertInterval = _random.Next(16, 49);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        for (var i = 0; i < bytecode.Length; i++)
        {
            writer.Write(bytecode[i]);

            if (i > 0 && i % junkInsertInterval == 0)
            {
                var junkSize = _random.Next(2, 9);
                writer.Write((byte)0xFE);
                writer.Write((byte)junkSize);

                for (var j = 0; j < junkSize; j++)
                {
                    writer.Write((byte)_random.Next(256));
                }
            }
        }

        return stream.ToArray();
    }

    private byte[] ApplyStringObfuscation(byte[] bytecode)
    {
        var xorKey = (byte)_random.Next(1, 256);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(xorKey);

        for (var i = 0; i < bytecode.Length; i++)
        {
            writer.Write((byte)(bytecode[i] ^ xorKey));
        }

        return stream.ToArray();
    }

    private static byte[] BuildSubstitutionTable(int seed)
    {
        var table = new byte[256];

        for (var i = 0; i < 256; i++)
        {
            table[i] = (byte)i;
        }

        var random = new Random(seed);

        for (var i = 255; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (table[i], table[j]) = (table[j], table[i]);
        }

        return table;
    }

    #endregion
}

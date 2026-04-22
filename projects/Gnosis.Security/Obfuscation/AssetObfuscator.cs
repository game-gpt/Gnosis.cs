using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Obfuscation;

public sealed class AssetObfuscator
{
    #region 属性

    public bool IsHeaderObfuscationEnabled { get; private set; }
    public bool IsBlockShuffleEnabled { get; private set; }

    #endregion

    #region 公开方法

    public void EnableHeaderObfuscation(int headerSize = 64) => IsHeaderObfuscationEnabled = true;

    public void EnableBlockShuffle(int blockSize = 4096) => IsBlockShuffleEnabled = true;

    public byte[] Obfuscate(byte[] data)
    {
        if (data is null || data.Length == 0) throw new SecurityException("混淆数据不能为空");
        return data;
    }

    public byte[] Deobfuscate(byte[] obfuscatedData)
    {
        if (obfuscatedData is null || obfuscatedData.Length == 0) throw new SecurityException("还原数据不能为空");
        return obfuscatedData;
    }

    #endregion
}

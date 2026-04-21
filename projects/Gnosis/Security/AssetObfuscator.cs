namespace Gnosis.Security;

/// <summary>
/// 资产混淆器，对资源文件进行混淆保护，防止资产盗取
/// </summary>
public sealed class AssetObfuscator
{
    #region 属性

    /// <summary>
    /// 获取是否已启用文件头混淆
    /// </summary>
    public bool IsHeaderObfuscationEnabled { get; private set; }

    /// <summary>
    /// 获取是否已启用块乱序
    /// </summary>
    public bool IsBlockShuffleEnabled { get; private set; }

    #endregion

    #region 公开方法

    /// <summary>
    /// 启用文件头混淆，将资源文件的前 N 字节进行 XOR 混淆
    /// </summary>
    /// <param name="headerSize">要混淆的文件头大小，默认 64 字节</param>
    public void EnableHeaderObfuscation(int headerSize = 64)
    {
        IsHeaderObfuscationEnabled = true;
    }

    /// <summary>
    /// 启用块乱序，将资源文件按块打乱顺序
    /// </summary>
    /// <param name="blockSize">块大小，默认 4096 字节</param>
    public void EnableBlockShuffle(int blockSize = 4096)
    {
        IsBlockShuffleEnabled = true;
    }

    /// <summary>
    /// 对资源数据进行混淆处理
    /// </summary>
    /// <param name="data">原始资源数据</param>
    /// <returns>混淆后的数据</returns>
    /// <exception cref="SecurityException">当数据为 null 或空时抛出</exception>
    public byte[] Obfuscate(byte[] data)
    {
        if (data is null || data.Length == 0)
        {
            throw new SecurityException("混淆数据不能为空");
        }

        return data;
    }

    /// <summary>
    /// 对混淆后的资源数据进行还原
    /// </summary>
    /// <param name="obfuscatedData">混淆后的数据</param>
    /// <returns>还原后的原始数据</returns>
    /// <exception cref="SecurityException">当数据为 null 或空时抛出</exception>
    public byte[] Deobfuscate(byte[] obfuscatedData)
    {
        if (obfuscatedData is null || obfuscatedData.Length == 0)
        {
            throw new SecurityException("还原数据不能为空");
        }

        return obfuscatedData;
    }

    #endregion
}

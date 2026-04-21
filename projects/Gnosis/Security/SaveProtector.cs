using System.Security.Cryptography;
using System.Text;

namespace Gnosis.Security;

/// <summary>
/// 存档保护器，使用 AES-256-GCM 加密保护存档数据，绑定硬件指纹防止篡改与迁移
/// </summary>
public sealed class SaveProtector
{
    #region 常量

    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("Gnosis.SaveProtector.v1");

    #endregion

    #region 公开方法

    /// <summary>
    /// 加密存档数据，使用硬件标识绑定加密密钥
    /// </summary>
    /// <param name="data">待加密的原始存档数据</param>
    /// <param name="hardwareId">硬件标识，用于密钥派生和附加认证数据</param>
    /// <returns>加密后的字节数组，格式为 [nonce(12)][tag(16)][ciphertext]</returns>
    /// <exception cref="SecurityException">当输入参数为 null 或空时抛出</exception>
    public byte[] EncryptSave(byte[] data, string hardwareId)
    {
        ValidateInput(data, hardwareId);

        var key = DeriveKey(hardwareId);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var aad = Encoding.UTF8.GetBytes(hardwareId);
        var ciphertext = new byte[data.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, data, ciphertext, tag, aad);

        var result = new byte[NonceSize + TagSize + data.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, data.Length);

        return result;
    }

    /// <summary>
    /// 解密存档数据，验证硬件指纹后还原原始数据
    /// </summary>
    /// <param name="encryptedData">加密的存档数据，格式为 [nonce(12)][tag(16)][ciphertext]</param>
    /// <param name="hardwareId">硬件标识，用于密钥派生和附加认证数据</param>
    /// <returns>解密后的原始存档数据</returns>
    /// <exception cref="SecurityException">当输入参数为 null 或空，或硬件指纹验证失败时抛出</exception>
    public byte[] DecryptSave(byte[] encryptedData, string hardwareId)
    {
        ValidateInput(encryptedData, hardwareId);

        if (encryptedData.Length < NonceSize + TagSize)
        {
            throw new SecurityException("加密数据长度不足");
        }

        var key = DeriveKey(hardwareId);
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertext = new byte[encryptedData.Length - NonceSize - TagSize];

        Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(encryptedData, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(encryptedData, NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

        var aad = Encoding.UTF8.GetBytes(hardwareId);
        var plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, aad);
        }
        catch (AuthenticationTagMismatchException)
        {
            throw new SecurityException("存档硬件指纹验证失败");
        }

        return plaintext;
    }

    /// <summary>
    /// 基于环境信息生成硬件指纹，返回 SHA256 十六进制字符串
    /// </summary>
    /// <returns>硬件指纹的十六进制字符串表示</returns>
    public static string GetHardwareFingerprint()
    {
        var raw = $"{Environment.MachineName}|{Environment.UserName}|{Environment.OSVersion}|{Environment.ProcessorCount}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 使用 SHA256 从固定盐值和硬件标识派生 AES-256 密钥
    /// </summary>
    /// <param name="hardwareId">硬件标识</param>
    /// <returns>32 字节的 AES 密钥</returns>
    private static byte[] DeriveKey(string hardwareId)
    {
        var hardwareIdBytes = Encoding.UTF8.GetBytes(hardwareId);
        var combined = new byte[Salt.Length + hardwareIdBytes.Length];
        Buffer.BlockCopy(Salt, 0, combined, 0, Salt.Length);
        Buffer.BlockCopy(hardwareIdBytes, 0, combined, Salt.Length, hardwareIdBytes.Length);
        return SHA256.HashData(combined);
    }

    /// <summary>
    /// 验证输入参数是否有效
    /// </summary>
    /// <param name="data">数据字节数组</param>
    /// <param name="hardwareId">硬件标识</param>
    /// <exception cref="SecurityException">当输入参数为 null 或空时抛出</exception>
    private static void ValidateInput(byte[] data, string hardwareId)
    {
        if (data is null || data.Length == 0)
        {
            throw new SecurityException("数据不能为空");
        }

        if (string.IsNullOrEmpty(hardwareId))
        {
            throw new SecurityException("硬件标识不能为空");
        }
    }

    #endregion
}

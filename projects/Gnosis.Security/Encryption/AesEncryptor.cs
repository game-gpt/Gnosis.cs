using System.Security.Cryptography;
using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Encryption;

public sealed class AesEncryptor
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    public byte[] Encrypt(byte[] data, byte[] key)
    {
        ValidateKey(key);
        if (data is null || data.Length == 0) throw new SecurityException("加密数据不能为空");

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[data.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, data, ciphertext, tag);

        var result = new byte[NonceSize + TagSize + data.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, data.Length);
        return result;
    }

    public byte[] Decrypt(byte[] encryptedData, byte[] key)
    {
        ValidateKey(key);
        if (encryptedData is null || encryptedData.Length < NonceSize + TagSize) throw new SecurityException("加密数据格式无效");

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertext = new byte[encryptedData.Length - NonceSize - TagSize];

        Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(encryptedData, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(encryptedData, NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

        var plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (AuthenticationTagMismatchException)
        {
            throw new SecurityException("AES 解密失败：认证标签不匹配");
        }

        return plaintext;
    }

    public static byte[] GenerateKey() => RandomNumberGenerator.GetBytes(KeySize);

    private static void ValidateKey(byte[] key)
    {
        if (key is null || key.Length != KeySize)
        {
            throw new SecurityException($"AES 密钥长度必须为 {KeySize} 字节");
        }
    }
}

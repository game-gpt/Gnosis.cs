using System.Security.Cryptography;
using System.Text;
using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Obfuscation;

public sealed class StringEncryptor
{
    public StringEncryptionResult Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) throw new SecurityException("加密字符串不能为空");

        var key = RandomNumberGenerator.GetBytes(32);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var encryptedData = new byte[12 + 16 + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, encryptedData, 0, 12);
        Buffer.BlockCopy(tag, 0, encryptedData, 12, 16);
        Buffer.BlockCopy(ciphertext, 0, encryptedData, 28, ciphertext.Length);

        return new StringEncryptionResult(encryptedData, key);
    }

    public string Decrypt(byte[] encryptedData, byte[] key)
    {
        if (encryptedData is null || encryptedData.Length < 28) throw new SecurityException("加密数据格式无效");
        if (key is null || key.Length != 32) throw new SecurityException("密钥长度必须为 32 字节");

        var nonce = new byte[12];
        var tag = new byte[16];
        var ciphertext = new byte[encryptedData.Length - 28];

        Buffer.BlockCopy(encryptedData, 0, nonce, 0, 12);
        Buffer.BlockCopy(encryptedData, 12, tag, 0, 16);
        Buffer.BlockCopy(encryptedData, 28, ciphertext, 0, ciphertext.Length);

        var plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (AuthenticationTagMismatchException)
        {
            throw new SecurityException("字符串解密失败：认证标签不匹配");
        }

        return Encoding.UTF8.GetString(plaintext);
    }
}

public sealed record StringEncryptionResult(byte[] EncryptedData, byte[] Key);

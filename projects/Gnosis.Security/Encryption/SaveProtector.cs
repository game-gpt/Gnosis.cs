using System.Security.Cryptography;
using System.Text;
using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Encryption;

public sealed class SaveProtector
{
    #region 常量

    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("Gnosis.SaveProtector.v1");

    #endregion

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

    public static string GetHardwareFingerprint()
    {
        var raw = $"{Environment.MachineName}|{Environment.UserName}|{Environment.OSVersion}|{Environment.ProcessorCount}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static byte[] DeriveKey(string hardwareId)
    {
        var hardwareIdBytes = Encoding.UTF8.GetBytes(hardwareId);
        var combined = new byte[Salt.Length + hardwareIdBytes.Length];
        Buffer.BlockCopy(Salt, 0, combined, 0, Salt.Length);
        Buffer.BlockCopy(hardwareIdBytes, 0, combined, Salt.Length, hardwareIdBytes.Length);
        return SHA256.HashData(combined);
    }

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
}

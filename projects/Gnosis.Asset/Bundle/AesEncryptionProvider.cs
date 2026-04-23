using System.Security.Cryptography;

namespace Gnosis.Asset.Bundle;

public sealed class AesEncryptionProvider : IEncryptionProvider
{
    private const int KeySizeBits = 256;
    private const int BlockSizeBytes = 16;
    private const int KeySizeBytes = KeySizeBits / 8;

    public EncryptionType EncryptionType => EncryptionType.Aes256Cbc;

    public EncryptedData Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
    {
        ValidateKey(key);

        var iv = new byte[BlockSizeBytes];
        RandomNumberGenerator.Fill(iv);

        using var aes = Aes.Create();
        aes.KeySize = KeySizeBits;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key.ToArray();
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(data.ToArray(), 0, data.Length);

        return new EncryptedData
        {
            Ciphertext = ciphertext,
            Iv = iv,
            EncryptionType = EncryptionType.Aes256Cbc
        };
    }

    public byte[] Decrypt(EncryptedData encryptedData, ReadOnlySpan<byte> key)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);

        ValidateKey(key);

        if (encryptedData.Iv.Length != BlockSizeBytes)
        {
            throw new ArgumentException($"IV 长度必须为 {BlockSizeBytes} 字节", nameof(encryptedData));
        }

        using var aes = Aes.Create();
        aes.KeySize = KeySizeBits;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key.ToArray();
        aes.IV = encryptedData.Iv;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(encryptedData.Ciphertext, 0, encryptedData.Ciphertext.Length);
    }

    private static void ValidateKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySizeBytes)
        {
            throw new ArgumentException($"AES-256 密钥长度必须为 {KeySizeBytes} 字节，当前为 {key.Length} 字节", nameof(key));
        }
    }
}

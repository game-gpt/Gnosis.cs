using System.Security.Cryptography;
using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Encryption;

public sealed class RsaEncryptor
{
    private const int DefaultKeySize = 2048;

    public (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair(int keySize = DefaultKeySize)
    {
        using var rsa = RSA.Create(keySize);
        var publicKey = rsa.ExportSubjectPublicKeyInfo();
        var privateKey = rsa.ExportPkcs8PrivateKey();
        return (publicKey, privateKey);
    }

    public byte[] Encrypt(byte[] data, byte[] publicKey)
    {
        if (data is null || data.Length == 0) throw new SecurityException("加密数据不能为空");

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        }
        catch (CryptographicException ex)
        {
            throw new SecurityException($"RSA 加密失败：{ex.Message}");
        }
    }

    public byte[] Decrypt(byte[] encryptedData, byte[] privateKey)
    {
        if (encryptedData is null || encryptedData.Length == 0) throw new SecurityException("解密数据不能为空");

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKey, out _);
            return rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
        }
        catch (CryptographicException ex)
        {
            throw new SecurityException($"RSA 解密失败：{ex.Message}");
        }
    }

    public byte[] Sign(byte[] data, byte[] privateKey)
    {
        if (data is null || data.Length == 0) throw new SecurityException("签名数据不能为空");

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKey, out _);
            return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (CryptographicException ex)
        {
            throw new SecurityException($"RSA 签名失败：{ex.Message}");
        }
    }

    public bool Verify(byte[] data, byte[] signature, byte[] publicKey)
    {
        if (data is null || signature is null || publicKey is null) return false;

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}

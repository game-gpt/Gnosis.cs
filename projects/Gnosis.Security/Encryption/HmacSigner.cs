using System.Security.Cryptography;
using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Encryption;

public sealed class HmacSigner
{
    private const int HmacSha256HashSize = 32;

    public byte[] Sign(byte[] data, byte[] key)
    {
        if (data is null || data.Length == 0) throw new SecurityException("签名数据不能为空");
        if (key is null || key.Length == 0) throw new SecurityException("签名密钥不能为空");

        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    public bool Verify(byte[] data, byte[] signature, byte[] key)
    {
        if (data is null || signature is null || key is null) return false;
        if (signature.Length != HmacSha256HashSize) return false;

        var computed = Sign(data, key);
        return CryptographicOperations.FixedTimeEquals(computed, signature);
    }

    public static byte[] GenerateKey() => RandomNumberGenerator.GetBytes(HmacSha256HashSize);
}

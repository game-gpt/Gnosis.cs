using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Encryption;

public class MemoryEncryptor : IMemoryProtector
{
    #region 字段

    private readonly byte[] _encryptionKey;
    private readonly byte[] _obfuscationKey;

    #endregion

    #region 构造函数

    public MemoryEncryptor()
    {
        _encryptionKey = GenerateKey(32);
        _obfuscationKey = GenerateKey(16);
    }

    #endregion

    #region 加密与解密

    public byte[] Encrypt(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            throw new SecurityException("加密数据不能为 null 或空数组");
        }

        var result = new byte[1 + _encryptionKey.Length + data.Length];
        result[0] = (byte)_encryptionKey.Length;
        Buffer.BlockCopy(_encryptionKey, 0, result, 1, _encryptionKey.Length);

        for (var i = 0; i < data.Length; i++)
        {
            result[1 + _encryptionKey.Length + i] = (byte)(data[i] ^ _encryptionKey[i % _encryptionKey.Length]);
        }

        return result;
    }

    public byte[] Decrypt(byte[] encryptedData)
    {
        if (encryptedData == null || encryptedData.Length == 0)
        {
            throw new SecurityException("解密数据不能为 null 或空数组");
        }

        int keyLength = encryptedData[0];

        if (encryptedData.Length < 1 + keyLength)
        {
            throw new SecurityException("加密数据格式无效：密钥数据不完整");
        }

        var key = new byte[keyLength];
        Buffer.BlockCopy(encryptedData, 1, key, 0, keyLength);

        var dataLength = encryptedData.Length - 1 - keyLength;
        var result = new byte[dataLength];

        for (var i = 0; i < dataLength; i++)
        {
            result[i] = (byte)(encryptedData[1 + keyLength + i] ^ key[i % keyLength]);
        }

        return result;
    }

    #endregion

    #region 混淆与还原

    public T Obfuscate<T>(T value) where T : struct
    {
        var size = Unsafe.SizeOf<T>();
        var bytes = new byte[size];
        MemoryMarshal.Write(bytes, in value);

        for (var i = 0; i < size; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ _obfuscationKey[i % _obfuscationKey.Length]);
        }

        return MemoryMarshal.Read<T>(bytes);
    }

    public T Deobfuscate<T>(T obfuscatedValue) where T : struct
    {
        var size = Unsafe.SizeOf<T>();
        var bytes = new byte[size];
        MemoryMarshal.Write(bytes, in obfuscatedValue);

        for (var i = 0; i < size; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ _obfuscationKey[i % _obfuscationKey.Length]);
        }

        return MemoryMarshal.Read<T>(bytes);
    }

    #endregion

    #region 蜜罐生成

    public byte[] GenerateHoneypot()
    {
        var result = new byte[16];
        var offset = 0;

        BitConverter.TryWriteBytes(result.AsSpan(offset, 4), 99999);
        offset += 4;

        BitConverter.TryWriteBytes(result.AsSpan(offset, 4), 100);
        offset += 4;

        BitConverter.TryWriteBytes(result.AsSpan(offset, 4), 1.0f);
        offset += 4;

        BitConverter.TryWriteBytes(result.AsSpan(offset, 4), 99999);

        return result;
    }

    #endregion

    private static byte[] GenerateKey(int length)
    {
        var key = new byte[length];
        Random.Shared.NextBytes(key);
        return key;
    }
}

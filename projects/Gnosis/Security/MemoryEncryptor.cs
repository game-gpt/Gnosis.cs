using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Gnosis.Security;

/// <summary>
/// 提供基于 XOR 的内存加密器，用于保护内存中的敏感数据免受扫描和篡改
/// </summary>
public class MemoryEncryptor : IMemoryProtector
{
    #region 字段

    private readonly byte[] _encryptionKey;
    private readonly byte[] _obfuscationKey;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 MemoryEncryptor 的新实例，生成随机加密密钥和混淆密钥
    /// </summary>
    public MemoryEncryptor()
    {
        _encryptionKey = GenerateKey(32);
        _obfuscationKey = GenerateKey(16);
    }

    #endregion

    #region 加密与解密

    /// <summary>
    /// 使用 XOR 加密指定的字节数据
    /// </summary>
    /// <param name="data">要加密的原始数据</param>
    /// <returns>加密后的字节数组，格式为 [密钥长度(1字节)][密钥(N字节)][加密数据]</returns>
    /// <exception cref="SecurityException">当数据为 null 或空数组时抛出</exception>
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

    /// <summary>
    /// 使用 XOR 解密指定的加密数据
    /// </summary>
    /// <param name="encryptedData">要解密的加密数据</param>
    /// <returns>解密后的原始字节数组</returns>
    /// <exception cref="SecurityException">当数据为 null 或空数组，或数据格式无效时抛出</exception>
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

    /// <summary>
    /// 对值类型进行混淆处理，使其在内存中的表现形式与原始值不同
    /// </summary>
    /// <typeparam name="T">要混淆的值类型</typeparam>
    /// <param name="value">要混淆的原始值</param>
    /// <returns>混淆后的值</returns>
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

    /// <summary>
    /// 对混淆后的值类型进行还原处理，恢复为原始值
    /// </summary>
    /// <typeparam name="T">要还原的值类型</typeparam>
    /// <param name="obfuscatedValue">混淆后的值</param>
    /// <returns>还原后的原始值</returns>
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

    /// <summary>
    /// 生成蜜罐数据，模拟常见游戏数据模式以诱骗内存扫描器
    /// </summary>
    /// <returns>包含模拟游戏数据的字节数组，格式为 [金币(4字节)][等级(4字节)][速度(4字节)][生命值(4字节)]</returns>
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

    #region 私有方法

    /// <summary>
    /// 生成指定长度的随机密钥
    /// </summary>
    /// <param name="length">密钥的字节长度</param>
    /// <returns>随机生成的密钥字节数组</returns>
    private static byte[] GenerateKey(int length)
    {
        var key = new byte[length];
        Random.Shared.NextBytes(key);
        return key;
    }

    #endregion
}

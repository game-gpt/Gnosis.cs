using System.Security.Cryptography;
using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Encryption;

public sealed class KeyManager
{
    #region 字段

    private readonly Dictionary<string, KeyEntry> _keys = new();
    private readonly byte[] _masterKey;

    #endregion

    #region 属性

    public int KeyCount => _keys.Count;

    #endregion

    #region 构造函数

    public KeyManager()
    {
        _masterKey = RandomNumberGenerator.GetBytes(32);
    }

    public KeyManager(byte[] masterKey)
    {
        _masterKey = masterKey ?? throw new SecurityException("主密钥不能为空");
    }

    #endregion

    #region 公开方法

    public void Store(string keyId, byte[] key)
    {
        if (string.IsNullOrEmpty(keyId)) throw new SecurityException("密钥标识不能为空");
        if (key is null || key.Length == 0) throw new SecurityException("密钥不能为空");

        var encryptedKey = EncryptKey(key);
        _keys[keyId] = new KeyEntry(encryptedKey, false, Environment.TickCount64);
    }

    public byte[] Retrieve(string keyId)
    {
        if (!_keys.TryGetValue(keyId, out var entry)) throw new SecurityException($"密钥 '{keyId}' 不存在");
        if (entry.IsExpired) throw new SecurityException($"密钥 '{keyId}' 已过期");

        return DecryptKey(entry.EncryptedKey);
    }

    public void Rotate(string keyId, byte[]? newKey = null)
    {
        if (!_keys.TryGetValue(keyId, out var entry)) throw new SecurityException($"密钥 '{keyId}' 不存在");

        _keys[keyId] = entry with { IsExpired = true };

        var rotatedKeyId = $"{keyId}_v{Environment.TickCount64}";
        var key = newKey ?? RandomNumberGenerator.GetBytes(32);
        Store(rotatedKeyId, key);
    }

    public bool IsValid(string keyId) => _keys.TryGetValue(keyId, out var entry) && !entry.IsExpired;

    public void Remove(string keyId) => _keys.Remove(keyId);

    #endregion

    #region 私有方法

    private byte[] EncryptKey(byte[] key)
    {
        var result = new byte[key.Length];
        for (var i = 0; i < key.Length; i++)
        {
            result[i] = (byte)(key[i] ^ _masterKey[i % _masterKey.Length]);
        }
        return result;
    }

    private byte[] DecryptKey(byte[] encryptedKey)
    {
        var result = new byte[encryptedKey.Length];
        for (var i = 0; i < encryptedKey.Length; i++)
        {
            result[i] = (byte)(encryptedKey[i] ^ _masterKey[i % _masterKey.Length]);
        }
        return result;
    }

    #endregion

    private sealed record KeyEntry(byte[] EncryptedKey, bool IsExpired, long CreatedAt);
}

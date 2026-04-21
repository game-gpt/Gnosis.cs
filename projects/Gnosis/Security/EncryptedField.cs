using System;

namespace Gnosis.Security;

/// <summary>
/// 加密字段结构体，在内存中对值类型数据进行混淆保护，防止内存扫描器读取敏感数据
/// </summary>
/// <typeparam name="T">要加密的值类型，必须是值类型</typeparam>
public struct EncryptedField<T> : IEncryptedField<T> where T : struct
{
    #region 私有字段

    private static readonly Lazy<MemoryEncryptor> _encryptor = new(() => new MemoryEncryptor());

    private T _encryptedValue;
    private T _originalValue;
    private bool _hasValue;

    #endregion

    #region 构造函数

    /// <summary>
    /// 默认构造函数，创建未赋值的加密字段
    /// </summary>
    public EncryptedField()
    {
        _hasValue = false;
        _encryptedValue = default;
        _originalValue = default;
    }

    /// <summary>
    /// 使用初始值初始化加密字段，同时记录原始值
    /// </summary>
    /// <param name="initialValue">初始值</param>
    public EncryptedField(T initialValue)
    {
        _hasValue = false;
        _encryptedValue = default;
        _originalValue = default;
        Value = initialValue;
    }

    #endregion

    #region 属性

    /// <summary>
    /// 获取或设置加密字段的值。获取时解密，设置时加密存储。
    /// </summary>
    public T Value
    {
        get
        {
            if (!_hasValue)
            {
                return default;
            }

            return _encryptor.Value.Deobfuscate(_encryptedValue);
        }
        set
        {
            if (!_hasValue)
            {
                _originalValue = value;
            }

            _encryptedValue = _encryptor.Value.Obfuscate(value);
            _hasValue = true;
        }
    }

    /// <summary>
    /// 获取加密字段的原始值，仅在首次赋值时记录，后续赋值不会更新此值
    /// </summary>
    public T OriginalValue => _originalValue;

    /// <summary>
    /// 获取是否已加密（即是否已赋值），未赋值时返回 false
    /// </summary>
    public bool IsEncrypted => _hasValue;

    #endregion
}

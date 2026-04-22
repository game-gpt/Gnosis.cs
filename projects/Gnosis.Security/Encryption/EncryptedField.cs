namespace Gnosis.Security.Encryption;

public struct EncryptedField<T> : IEncryptedField<T> where T : struct
{
    private static readonly Lazy<MemoryEncryptor> _encryptor = new(() => new MemoryEncryptor());

    private T _encryptedValue;
    private T _originalValue;
    private bool _hasValue;

    public EncryptedField()
    {
        _hasValue = false;
        _encryptedValue = default;
        _originalValue = default;
    }

    public EncryptedField(T initialValue)
    {
        _hasValue = false;
        _encryptedValue = default;
        _originalValue = default;
        Value = initialValue;
    }

    public T Value
    {
        get
        {
            if (!_hasValue) return default;
            return _encryptor.Value.Deobfuscate(_encryptedValue);
        }
        set
        {
            if (!_hasValue) _originalValue = value;
            _encryptedValue = _encryptor.Value.Obfuscate(value);
            _hasValue = true;
        }
    }

    public T OriginalValue => _originalValue;
    public bool IsEncrypted => _hasValue;
}

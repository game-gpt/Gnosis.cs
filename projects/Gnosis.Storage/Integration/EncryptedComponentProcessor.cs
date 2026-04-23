using System.Reflection;
using System.Text.Json;
using Gnosis.ECS.Component;
using Gnosis.Security.Encryption;

namespace Gnosis.Storage.Integration;

public sealed class EncryptedComponentProcessor
{
    #region 字段

    private readonly AesEncryptor _encryptor;
    private readonly KeyManager _keyManager;
    private readonly Dictionary<Type, List<FieldInfo>> _encryptedFieldCache = new();

    #endregion

    #region 构造函数

    public EncryptedComponentProcessor()
    {
        _encryptor = new AesEncryptor();
        _keyManager = new KeyManager();

        var defaultKey = AesEncryptor.GenerateKey();
        _keyManager.Store("default", defaultKey);
    }

    public EncryptedComponentProcessor(AesEncryptor encryptor, KeyManager keyManager)
    {
        _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
        _keyManager = keyManager ?? throw new ArgumentNullException(nameof(keyManager));
    }

    #endregion

    #region 公开方法

    public string ProcessSerialize<T>(T component, ComponentSerializer serializer) where T : struct
    {
        var json = serializer.Serialize(component);
        var componentType = typeof(T);

        if (!HasEncryptedFields(componentType))
        {
            return json;
        }

        return EncryptJsonFields(json, componentType);
    }

    public T ProcessDeserialize<T>(string json, ComponentSerializer serializer) where T : struct
    {
        var componentType = typeof(T);

        if (!HasEncryptedFields(componentType))
        {
            return serializer.Deserialize<T>(json);
        }

        var decryptedJson = DecryptJsonFields(json, componentType);
        return serializer.Deserialize<T>(decryptedJson);
    }

    public string ProcessSerialize(object component, Type componentType, ComponentSerializer serializer)
    {
        var json = serializer.Serialize(component, componentType);

        if (!HasEncryptedFields(componentType))
        {
            return json;
        }

        return EncryptJsonFields(json, componentType);
    }

    public object ProcessDeserialize(string json, Type componentType, ComponentSerializer serializer)
    {
        if (!HasEncryptedFields(componentType))
        {
            return serializer.Deserialize(json, componentType);
        }

        var decryptedJson = DecryptJsonFields(json, componentType);
        return serializer.Deserialize(decryptedJson, componentType);
    }

    public bool HasEncryptedFields(Type componentType)
    {
        return GetEncryptedFields(componentType).Count > 0;
    }

    public IReadOnlyList<FieldInfo> GetEncryptedFields(Type componentType)
    {
        if (!_encryptedFieldCache.TryGetValue(componentType, out var fields))
        {
            fields = componentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<EncryptedAttribute>() != null)
                .ToList();

            _encryptedFieldCache[componentType] = fields;
        }

        return fields;
    }

    public void RegisterEncryptionKey(string keyId, byte[] key)
    {
        _keyManager.Store(keyId, key);
    }

    public void RotateEncryptionKey(string keyId, byte[]? newKey = null)
    {
        _keyManager.Rotate(keyId, newKey);
    }

    #endregion

    #region 私有方法

    private string EncryptJsonFields(string json, Type componentType)
    {
        var encryptedFields = GetEncryptedFields(componentType);

        if (encryptedFields.Count == 0)
        {
            return json;
        }

        using var doc = JsonDocument.Parse(json);
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);

        writer.WriteStartObject();

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            var isEncrypted = encryptedFields.Any(f =>
                string.Equals(f.Name, property.Name, StringComparison.OrdinalIgnoreCase));

            if (isEncrypted && property.Value.ValueKind == JsonValueKind.String)
            {
                var plainText = property.Value.GetString() ?? string.Empty;
                var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                var key = _keyManager.Retrieve("default")!;
                var encrypted = _encryptor.Encrypt(plainBytes, key);
                var encryptedBase64 = Convert.ToBase64String(encrypted);

                writer.WriteString(property.Name, $"enc:{encryptedBase64}");
            }
            else if (isEncrypted)
            {
                var rawValue = property.Value.GetRawText();
                var plainBytes = System.Text.Encoding.UTF8.GetBytes(rawValue);
                var key = _keyManager.Retrieve("default")!;
                var encrypted = _encryptor.Encrypt(plainBytes, key);
                var encryptedBase64 = Convert.ToBase64String(encrypted);

                writer.WriteString(property.Name, $"enc:{encryptedBase64}");
            }
            else
            {
                property.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(ms.ToArray());
    }

    private string DecryptJsonFields(string json, Type componentType)
    {
        var encryptedFields = GetEncryptedFields(componentType);

        if (encryptedFields.Count == 0)
        {
            return json;
        }

        using var doc = JsonDocument.Parse(json);
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);

        writer.WriteStartObject();

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            var isEncrypted = encryptedFields.Any(f =>
                string.Equals(f.Name, property.Name, StringComparison.OrdinalIgnoreCase));

            if (isEncrypted && property.Value.ValueKind == JsonValueKind.String)
            {
                var value = property.Value.GetString() ?? string.Empty;

                if (value.StartsWith("enc:"))
                {
                    var encryptedBase64 = value[4..];
                    var encryptedBytes = Convert.FromBase64String(encryptedBase64);
                    var key = _keyManager.Retrieve("default")!;
                    var decrypted = _encryptor.Decrypt(encryptedBytes, key);
                    var decryptedValue = System.Text.Encoding.UTF8.GetString(decrypted);

                    writer.WritePropertyName(property.Name);
                    using var innerDoc = JsonDocument.Parse(decryptedValue);
                    innerDoc.RootElement.WriteTo(writer);
                }
                else
                {
                    property.WriteTo(writer);
                }
            }
            else
            {
                property.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(ms.ToArray());
    }

    #endregion
}

using Gnosis.Security.AntiCheat;
using Gnosis.Security.Encryption;
using Gnosis.Storage.Provider;

namespace Gnosis.Storage.Save;

public sealed class SaveSystem
{
    #region 常量

    private const int HmacSignatureSize = 32;
    private const int FormatVersion = 2;

    #endregion

    #region 字段

    private readonly IStorageProvider _provider;
    private readonly Dictionary<string, SaveSlot> _slots = new();
    private readonly SaveProtector? _saveProtector;
    private readonly HmacSigner? _hmacSigner;
    private readonly byte[]? _hmacKey;

    #endregion

    #region 属性

    public int SlotCount => _slots.Count;

    public bool AutoSaveEnabled { get; set; }

    public float AutoSaveInterval { get; set; } = 300f;

    public bool EncryptionEnabled => _saveProtector != null;

    public bool IntegrityCheckEnabled => _hmacSigner != null;

    #endregion

    #region 构造函数

    public SaveSystem(IStorageProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public SaveSystem(IStorageProvider provider, bool enableEncryption, bool enableIntegrityCheck = true)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        if (enableEncryption)
        {
            _saveProtector = new SaveProtector();
        }

        if (enableIntegrityCheck)
        {
            _hmacSigner = new HmacSigner();
            _hmacKey = HmacSigner.GenerateKey();
        }
    }

    public SaveSystem(IStorageProvider provider, SaveProtector saveProtector, HmacSigner hmacSigner, byte[] hmacKey)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _saveProtector = saveProtector ?? throw new ArgumentNullException(nameof(saveProtector));
        _hmacSigner = hmacSigner ?? throw new ArgumentNullException(nameof(hmacSigner));
        _hmacKey = hmacKey ?? throw new ArgumentNullException(nameof(hmacKey));
    }

    #endregion

    #region 公开方法

    public SaveSlot CreateSlot(int index, string name)
    {
        var slot = new SaveSlot(index, name);
        _slots[$"slot_{index}"] = slot;
        return slot;
    }

    public SaveSlot? GetSlot(int index)
    {
        return _slots.TryGetValue($"slot_{index}", out var slot) ? slot : null;
    }

    public void DeleteSlot(int index)
    {
        _slots.Remove($"slot_{index}");
    }

    public async Task SaveAsync(int slotIndex)
    {
        var slot = GetSlot(slotIndex);

        if (slot is null)
        {
            return;
        }

        var data = slot.Serialize();
        data = ApplyProtection(data, slotIndex);
        await _provider.SaveAsync($"save_slot_{slotIndex}", data);
    }

    public async Task<bool> LoadAsync(int slotIndex)
    {
        var data = await _provider.LoadAsync($"save_slot_{slotIndex}");

        if (data is null)
        {
            return false;
        }

        data = RemoveProtection(data, slotIndex);

        if (data is null)
        {
            return false;
        }

        var slot = GetSlot(slotIndex);

        if (slot is not null)
        {
            slot.Deserialize(data);
        }

        return true;
    }

    #endregion

    #region 保护处理

    private byte[] ApplyProtection(byte[] data, int slotIndex)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(FormatVersion);

        var payload = data;

        if (_saveProtector != null)
        {
            var hardwareId = SaveProtector.GetHardwareFingerprint();
            payload = _saveProtector.EncryptSave(data, hardwareId);
        }

        if (_hmacSigner != null && _hmacKey != null)
        {
            var signature = _hmacSigner.Sign(payload, _hmacKey);
            writer.Write(signature.Length);
            writer.Write(signature);
        }
        else
        {
            writer.Write(0);
        }

        writer.Write(payload.Length);
        writer.Write(payload);

        return ms.ToArray();
    }

    private byte[]? RemoveProtection(byte[] data, int slotIndex)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var formatVersion = reader.ReadInt32();

        if (formatVersion < 1 || formatVersion > FormatVersion)
        {
            return null;
        }

        var signatureLength = reader.ReadInt32();

        if (signatureLength > 0 && _hmacSigner != null && _hmacKey != null)
        {
            var signature = reader.ReadBytes(signatureLength);
            var payloadLength = reader.ReadInt32();
            var payload = reader.ReadBytes(payloadLength);

            if (!_hmacSigner.Verify(payload, signature, _hmacKey))
            {
                return null;
            }

            return DecryptPayload(payload);
        }

        var dataLength = reader.ReadInt32();
        var rawPayload = reader.ReadBytes(dataLength);

        return DecryptPayload(rawPayload);
    }

    private byte[]? DecryptPayload(byte[] payload)
    {
        if (_saveProtector == null)
        {
            return payload;
        }

        try
        {
            var hardwareId = SaveProtector.GetHardwareFingerprint();
            return _saveProtector.DecryptSave(payload, hardwareId);
        }
        catch (SecurityException)
        {
            return null;
        }
    }

    #endregion
}

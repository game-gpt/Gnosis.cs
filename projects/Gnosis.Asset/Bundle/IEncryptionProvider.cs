namespace Gnosis.Asset.Bundle;

public interface IEncryptionProvider
{
    EncryptionType EncryptionType { get; }

    EncryptedData Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key);

    byte[] Decrypt(EncryptedData encryptedData, ReadOnlySpan<byte> key);
}

public sealed class EncryptedData
{
    public required byte[] Ciphertext { get; init; }
    public required byte[] Iv { get; init; }
    public required EncryptionType EncryptionType { get; init; }
}

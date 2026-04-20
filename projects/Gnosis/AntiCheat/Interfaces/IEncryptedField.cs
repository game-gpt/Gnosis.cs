namespace Gnosis.AntiCheat.Interfaces;

public interface IEncryptedField<T> where T : struct
{
    T Value { get; set; }
    T OriginalValue { get; }
    bool IsEncrypted { get; }
}

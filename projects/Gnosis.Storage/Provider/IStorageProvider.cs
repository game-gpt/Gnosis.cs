namespace Gnosis.Storage.Provider;

public interface IStorageProvider
{
    string Name { get; }
    Task<bool> SaveAsync(string key, byte[] data);
    Task<byte[]?> LoadAsync(string key);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<string[]> ListKeysAsync(string prefix);
}

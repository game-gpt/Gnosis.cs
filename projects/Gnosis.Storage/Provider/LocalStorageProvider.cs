namespace Gnosis.Storage.Provider;

public sealed class LocalStorageProvider : IStorageProvider
{
    #region 字段

    private readonly Dictionary<string, byte[]> _data = new();

    #endregion

    #region 属性

    public string Name => "Local";

    #endregion

    #region IStorageProvider 实现

    public Task<bool> SaveAsync(string key, byte[] data)
    {
        _data[key] = data;
        return Task.FromResult(true);
    }

    public Task<byte[]?> LoadAsync(string key)
    {
        return Task.FromResult(_data.TryGetValue(key, out var data) ? data : null);
    }

    public Task<bool> DeleteAsync(string key)
    {
        return Task.FromResult(_data.Remove(key));
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(_data.ContainsKey(key));
    }

    public Task<string[]> ListKeysAsync(string prefix)
    {
        var keys = _data.Keys.Where(k => k.StartsWith(prefix)).ToArray();
        return Task.FromResult(keys);
    }

    #endregion
}

namespace Gnosis.Storage.Provider;

[Obsolete("请使用 DatabaseStorageProvider，通过 Gnosis.Database 统一访问存储层")]
public sealed class LocalStorageProvider : IStorageProvider, IAsyncDisposable
{
    #region 字段

    private readonly LightDB.LightDatabase _db;
    private bool _disposed;

    #endregion

    #region 构造函数

    public LocalStorageProvider(string path = ".gnosis/storage")
    {
        _db = new LightDB.LightDatabase(new LightDB.Core.LightOptions
        {
            Path = path
        });
    }

    #endregion

    #region 属性

    public string Name => "LightDB";

    #endregion

    #region IStorageProvider 实现

    public async Task<bool> SaveAsync(string key, byte[] data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var lightKey = LightDB.Core.LightKey.FromString(key);
        var lightValue = new LightDB.Core.LightValue(data);
        await _db.PutAsync(lightKey, lightValue);
        return true;
    }

    public async Task<byte[]?> LoadAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var lightKey = LightDB.Core.LightKey.FromString(key);
        var result = await _db.GetAsync<LightDB.Core.LightValue>(lightKey);
        if (result.IsEmpty)
        {
            return null;
        }

        return result.Bytes.ToArray();
    }

    public async Task<bool> DeleteAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var lightKey = LightDB.Core.LightKey.FromString(key);
        return await _db.DeleteAsync(lightKey);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var lightKey = LightDB.Core.LightKey.FromString(key);
        return await _db.ExistsAsync(lightKey);
    }

    public Task<string[]> ListKeysAsync(string prefix)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var lightPrefix = LightDB.Core.LightKey.FromString(prefix);
        var cursor = _db.Seek(lightPrefix);
        var keys = new List<string>();

        while (cursor.MoveNextAsync().GetAwaiter().GetResult())
        {
            var keyStr = System.Text.Encoding.UTF8.GetString(cursor.Current.Key.Bytes.ToArray());
            if (keyStr.StartsWith(prefix))
            {
                keys.Add(keyStr);
            }
        }

        cursor.Dispose();
        return Task.FromResult(keys.ToArray());
    }

    #endregion

    #region IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _db.DisposeAsync();
    }

    #endregion
}

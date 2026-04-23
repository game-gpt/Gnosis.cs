using SolidDB.Core;

namespace Gnosis.Storage.Provider;

[Obsolete("请使用 DatabaseStorageProvider，通过 Gnosis.Database 统一访问存储层")]
public sealed class LocalStorageProvider : IStorageProvider, IAsyncDisposable
{
    #region 字段

    private readonly SolidDB.SolidDatabase _db;
    private bool _disposed;

    #endregion

    #region 构造函数

    public LocalStorageProvider(string path = ".gnosis/storage")
    {
        _db = new SolidDB.SolidDatabase(new SolidOptions
        {
            Path = path
        });
    }

    #endregion

    #region 属性

    public string Name => "SolidDB";

    #endregion

    #region IStorageProvider 实现

    public async Task<bool> SaveAsync(string key, byte[] data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var solidKey = SolidKey.FromString(key);
        var solidValue = new SolidValue(data);
        await _db.PutAsync(solidKey, solidValue);
        return true;
    }

    public async Task<byte[]?> LoadAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var solidKey = SolidKey.FromString(key);
        var result = await _db.GetAsync<SolidValue>(solidKey);
        if (result.IsEmpty)
        {
            return null;
        }

        return result.Bytes.ToArray();
    }

    public async Task<bool> DeleteAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var solidKey = SolidKey.FromString(key);
        return await _db.DeleteAsync(solidKey);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var solidKey = SolidKey.FromString(key);
        return await _db.ExistsAsync(solidKey);
    }

    public Task<string[]> ListKeysAsync(string prefix)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var solidPrefix = SolidKey.FromString(prefix);
        var cursor = _db.Seek(solidPrefix);
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

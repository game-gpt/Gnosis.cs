using Gnosis.Database.Core;

namespace Gnosis.Storage.Provider;

public sealed class DatabaseStorageProvider : IStorageProvider, IAsyncDisposable
{
    #region 字段

    private readonly IKvDatabase _database;
    private bool _disposed;

    #endregion

    #region 构造函数

    public DatabaseStorageProvider(IKvDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    #endregion

    #region 属性

    public string Name => "Gnosis.Database";

    #endregion

    #region IStorageProvider 实现

    public async Task<bool> SaveAsync(string key, byte[] data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var dbKey = DatabaseKey.FromString(key);
        var dbValue = new DatabaseValue(data);
        await _database.PutAsync(dbKey, dbValue);
        return true;
    }

    public async Task<byte[]?> LoadAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var dbKey = DatabaseKey.FromString(key);
        var result = await _database.GetAsync(dbKey);

        if (result is not { IsEmpty: false })
        {
            return null;
        }

        return result.Value.Bytes.ToArray();
    }

    public async Task<bool> DeleteAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var dbKey = DatabaseKey.FromString(key);
        return await _database.DeleteAsync(dbKey);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var dbKey = DatabaseKey.FromString(key);
        return await _database.ExistsAsync(dbKey);
    }

    public Task<string[]> ListKeysAsync(string prefix)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var dbPrefix = DatabaseKey.FromString(prefix);
        var keys = new List<string>();

        using var cursor = _database.Seek(dbPrefix);

        while (cursor.MoveNext())
        {
            var keyStr = cursor.Current.Key.ToString();
            if (keyStr.StartsWith(prefix))
            {
                keys.Add(keyStr);
            }
        }

        return Task.FromResult(keys.ToArray());
    }

    #endregion

    #region IAsyncDisposable

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;

        _database.Dispose();
        return ValueTask.CompletedTask;
    }

    #endregion
}

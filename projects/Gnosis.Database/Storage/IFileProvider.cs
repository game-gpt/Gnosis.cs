namespace Gnosis.Database.Storage;

public interface IFileProvider : IDisposable, IAsyncDisposable
{
    ValueTask<IStorageEngine> CreateEngineAsync(string path, StorageOptions options, CancellationToken cancellationToken = default);

    ValueTask<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(string path, CancellationToken cancellationToken = default);

    ValueTask<long> GetSizeAsync(string path, CancellationToken cancellationToken = default);
}

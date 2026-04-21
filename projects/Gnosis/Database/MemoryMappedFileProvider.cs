using Gnosis.Database.Storage;

namespace Gnosis.Database;

public sealed class MemoryMappedFileProvider : IFileProvider
{
    public ValueTask<IStorageEngine> CreateEngineAsync(string path, StorageOptions options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<long> GetSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

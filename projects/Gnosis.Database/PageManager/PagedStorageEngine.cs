using Gnosis.Database.Core;
using Gnosis.Database.Storage;

namespace Gnosis.Database.PageManager;

public sealed class PagedStorageEngine : IStorageEngine
{
    public StorageEngineType Type => StorageEngineType.MemoryMappedFile;

    public long Length => throw new NotImplementedException();

    public ValueTask CompactAsync(CompactionMode mode, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask InitializeAsync(StorageOptions options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<int> ReadAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask WriteAsync(long offset, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
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

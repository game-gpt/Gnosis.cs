using Gnosis.Database.Core;
using Gnosis.Database.Storage;

namespace Gnosis.Database.Engine;

public interface IStorageEngine : IDisposable, IAsyncDisposable
{
    StorageEngineType Type { get; }

    ValueTask InitializeAsync(StorageOptions options, CancellationToken cancellationToken = default);

    ValueTask WriteAsync(long offset, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    ValueTask<int> ReadAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken = default);

    ValueTask FlushAsync(CancellationToken cancellationToken = default);

    ValueTask CompactAsync(CompactionMode mode, CancellationToken cancellationToken = default);

    long Length { get; }
}

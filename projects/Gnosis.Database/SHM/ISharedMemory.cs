namespace Gnosis.Database.SHM;

public interface ISharedMemory : IDisposable
{
    nint BaseAddress { get; }

    long Size { get; }

    string Name { get; }

    ValueTask<bool> InitializeAsync(long size, CancellationToken cancellationToken = default);

    ValueTask<IShmSegment> AllocateSegmentAsync(int size, CancellationToken cancellationToken = default);

    ValueTask FreeSegmentAsync(IShmSegment segment, CancellationToken cancellationToken = default);

    void Write(long offset, ReadOnlySpan<byte> data);

    void Read(long offset, Span<byte> destination);

    ValueTask FlushAsync(CancellationToken cancellationToken = default);

    ShmOptions Options { get; }
}

namespace Gnosis.Database.SHM;

public sealed class ShmCacheManager : ISharedMemory
{
    public ShmCacheManager(ShmOptions options)
    {
        Options = options;
    }

    public nint BaseAddress => throw new NotImplementedException();

    public long Size => throw new NotImplementedException();

    public string Name => Options.Name;

    public ShmOptions Options { get; }

    public ValueTask<IShmSegment> AllocateSegmentAsync(int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask FreeSegmentAsync(IShmSegment segment, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> InitializeAsync(long size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Read(long offset, Span<byte> destination)
    {
        throw new NotImplementedException();
    }

    public void Write(long offset, ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}

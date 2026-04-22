namespace Gnosis.Database.SHM;

public interface IShmSegment
{
    long Offset { get; }

    int Size { get; }

    bool IsAllocated { get; }

    void Write(ReadOnlySpan<byte> data);

    void Read(Span<byte> destination);
}

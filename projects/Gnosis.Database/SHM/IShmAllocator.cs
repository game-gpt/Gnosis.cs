namespace Gnosis.Database.SHM;

public interface IShmAllocator
{
    long Allocate(int size);

    void Free(long offset);

    void Defragment();

    long UsedSize { get; }

    long TotalSize { get; }

    double UsageRatio => TotalSize > 0 ? (double)UsedSize / TotalSize : 0;
}

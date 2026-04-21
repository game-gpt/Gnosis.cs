namespace Gnosis.Database.Core;

public readonly record struct DatabaseStatistics(
    long TotalKeys,
    long TotalReads,
    long TotalWrites,
    long CacheHits,
    long CacheMisses,
    long WalEntries,
    long FreePages,
    long UsedPages)
{
    public double CacheHitRate => TotalReads > 0 ? (double)CacheHits / TotalReads : 0;

    public double PageUsageRatio =>
        (UsedPages + FreePages) > 0 ? (double)UsedPages / (UsedPages + FreePages) : 0;

    public static readonly DatabaseStatistics Zero = new(0, 0, 0, 0, 0, 0, 0, 0);
}

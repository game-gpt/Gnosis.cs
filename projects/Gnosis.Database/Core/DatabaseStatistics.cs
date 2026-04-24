namespace Gnosis.Database.Core;

public sealed class DatabaseStatistics
{
    public long TotalKeys { get; set; }

    public long TotalReads { get; set; }

    public long TotalWrites { get; set; }

    public long CacheHits { get; set; }

    public long CacheMisses { get; set; }

    public long WalEntries { get; set; }

    public long FreePages { get; set; }

    public long UsedPages { get; set; }

    public double CacheHitRate => TotalReads > 0 ? (double)CacheHits / TotalReads : 0;

    public double PageUsageRatio =>
        (UsedPages + FreePages) > 0 ? (double)UsedPages / (UsedPages + FreePages) : 0;

    public static readonly DatabaseStatistics Zero = new();

    public static DatabaseStatistics FromLightStatistics(LightDB.Core.LightStatistics stats) => new()
    {
        TotalKeys = stats.TotalEntries,
        TotalReads = stats.ReadCount,
        TotalWrites = stats.WriteCount,
        CacheHits = stats.CacheHits,
        CacheMisses = stats.CacheMisses,
        UsedPages = stats.DatabaseSize / 4096
    };
}

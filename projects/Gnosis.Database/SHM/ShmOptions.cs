namespace Gnosis.Database.SHM;

public readonly record struct ShmOptions(
    string Name,
    long MaxSize,
    int PageSize,
    int MaxPageCount,
    double EvictionThreshold,
    bool EnableCrossProcess)
{
    public static readonly ShmOptions Default = new(
        Name: "genesis_db_shm",
        MaxSize: 256 * 1024 * 1024,
        PageSize: 4096,
        MaxPageCount: 65536,
        EvictionThreshold: 0.85,
        EnableCrossProcess: true);
}

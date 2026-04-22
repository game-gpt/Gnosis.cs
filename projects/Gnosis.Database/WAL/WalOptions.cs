namespace Gnosis.Database.WAL;

public readonly record struct WalOptions(
    string Directory,
    long MaxFileSize,
    bool SyncOnCommit,
    bool CompressionEnabled,
    int BufferSize)
{
    public static readonly WalOptions Default = new(
        Directory: ".genesis/wal",
        MaxFileSize: 64 * 1024 * 1024,
        SyncOnCommit: true,
        CompressionEnabled: false,
        BufferSize: 64 * 1024);
}

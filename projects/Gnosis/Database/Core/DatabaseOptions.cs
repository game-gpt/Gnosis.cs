using Gnosis.Database.SHM;
using Gnosis.Database.Storage;
using Gnosis.Database.WAL;

namespace Gnosis.Database.Core;

public readonly record struct DatabaseOptions(
    string Path,
    StorageOptions Storage,
    WalOptions Wal,
    ShmOptions Shm,
    int BTreeOrder,
    bool ReadOnly,
    bool AutoCheckpoint,
    int CheckpointIntervalMs)
{
    public static readonly DatabaseOptions Default = new(
        Path: ".genesis/db",
        Storage: StorageOptions.Default,
        Wal: WalOptions.Default,
        Shm: ShmOptions.Default,
        BTreeOrder: 128,
        ReadOnly: false,
        AutoCheckpoint: true,
        CheckpointIntervalMs: 30000);
}

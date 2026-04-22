using Gnosis.Database.Core;

namespace Gnosis.Database.Storage;

public readonly record struct StorageOptions(
    StorageEngineType EngineType,
    string BasePath,
    int PageSize,
    long InitialSize,
    long MaxSize,
    bool UseDirectIO,
    bool UseSparseFile)
{
    public static readonly StorageOptions Default = new(
        EngineType: StorageEngineType.MemoryMappedFile,
        BasePath: ".genesis/db",
        PageSize: 4096,
        InitialSize: 16 * 1024 * 1024,
        MaxSize: long.MaxValue,
        UseDirectIO: false,
        UseSparseFile: true);
}

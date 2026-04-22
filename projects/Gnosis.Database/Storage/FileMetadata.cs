using Gnosis.Database.Core;

namespace Gnosis.Database.Storage;

public readonly record struct FileMetadata(
    string Path,
    long Size,
    Timestamp CreatedAt,
    Timestamp ModifiedAt,
    StorageEngineType EngineType);

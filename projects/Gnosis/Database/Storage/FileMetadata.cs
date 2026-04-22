using Gnosis.Core;
using Gnosis.Database.Core;
using Gnosis.Infrastructure;

namespace Gnosis.Database.Storage;

public readonly record struct FileMetadata(
    string Path,
    long Size,
    Timestamp CreatedAt,
    Timestamp ModifiedAt,
    StorageEngineType EngineType);

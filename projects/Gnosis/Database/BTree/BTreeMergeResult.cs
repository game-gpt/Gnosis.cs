using Gnosis.Database.Core;

namespace Gnosis.Database.BTree;

public readonly record struct BTreeMergeResult(
    PageId MergedPageId,
    DatabaseKey RemovedKey);

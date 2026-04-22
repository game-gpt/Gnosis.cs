using Gnosis.Database.Core;

namespace Gnosis.Database.BTree;

public readonly record struct BTreeSplitResult(
    DatabaseKey MiddleKey,
    PageId LeftPageId,
    PageId RightPageId);

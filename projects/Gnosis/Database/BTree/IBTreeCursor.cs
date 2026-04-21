using Gnosis.Database.Core;

namespace Gnosis.Database.BTree;

public interface IBTreeCursor : IDisposable
{
    DatabaseEntry Current { get; }

    bool IsValid { get; }

    bool MoveNext();

    bool MovePrev();

    bool Seek(DatabaseKey key);

    bool SeekToFirst();

    bool SeekToLast();
}

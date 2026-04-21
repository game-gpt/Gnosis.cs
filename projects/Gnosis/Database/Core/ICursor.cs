namespace Gnosis.Database.Core;

public interface ICursor : IDisposable
{
    DatabaseEntry Current { get; }

    bool IsValid { get; }

    bool MoveNext();

    bool MovePrev();

    bool SeekToFirst();

    bool SeekToLast();

    bool Seek(DatabaseKey key);

    IReadOnlyList<DatabaseEntry> GetRange(DatabaseKey start, DatabaseKey end, int limit = 1000);
}

namespace Gnosis.Database.Core;

public interface ISnapshot : IDisposable
{
    SequenceNumber Sequence { get; }

    ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default);

    ICursor Seek(DatabaseKey key);

    ISnapshot CreateChild();
}

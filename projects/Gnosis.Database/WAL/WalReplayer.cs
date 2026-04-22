using Gnosis.Database.Core;

namespace Gnosis.Database.WAL;

public sealed class WalReplayer : IWalReplayer
{
    #region 公开方法

    public async ValueTask ReplayAsync(IWriteAheadLog wal, SequenceNumber fromSequence, CancellationToken cancellationToken = default)
    {
        var entries = await wal.ReadFromAsync(fromSequence, cancellationToken).ConfigureAwait(false);

        var committedEntries = FilterCommitted(entries);

        foreach (var entry in committedEntries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            ApplyEntry(entry);
        }
    }

    public async ValueTask<IReadOnlyList<WalEntry>> GetCommittedEntriesAsync(IWriteAheadLog wal, SequenceNumber fromSequence, CancellationToken cancellationToken = default)
    {
        var entries = await wal.ReadFromAsync(fromSequence, cancellationToken).ConfigureAwait(false);
        return FilterCommitted(entries);
    }

    #endregion

    #region 私有方法

    private static IReadOnlyList<WalEntry> FilterCommitted(IReadOnlyList<WalEntry> entries)
    {
        var committedTxns = new HashSet<ulong>();
        var result = new List<WalEntry>();

        foreach (var entry in entries)
        {
            if (entry.EntryType == WalEntryType.Commit)
            {
                committedTxns.Add(entry.TransactionId.Value);
            }
        }

        foreach (var entry in entries)
        {
            if (entry.EntryType == WalEntryType.Put || entry.EntryType == WalEntryType.Delete)
            {
                if (committedTxns.Contains(entry.TransactionId.Value))
                {
                    result.Add(entry);
                }
            }
        }

        return result;
    }

    private static void ApplyEntry(WalEntry entry)
    {
        switch (entry.EntryType)
        {
            case WalEntryType.Put:
                ApplyPut(entry);
                break;
            case WalEntryType.Delete:
                ApplyDelete(entry);
                break;
        }
    }

    private static void ApplyPut(WalEntry entry)
    {
    }

    private static void ApplyDelete(WalEntry entry)
    {
    }

    #endregion
}

namespace Gnosis.Database.WAL;

public enum WalEntryType : byte
{
    Put = 1,
    Delete = 2,
    Commit = 3,
    Rollback = 4,
    Checkpoint = 5
}

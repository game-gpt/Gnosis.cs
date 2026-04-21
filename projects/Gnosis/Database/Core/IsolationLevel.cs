namespace Gnosis.Database.Core;

public enum IsolationLevel
{
    ReadUncommitted,
    ReadCommitted,
    Snapshot,
    Serializable
}

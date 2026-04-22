namespace Gnosis.Database.Core;

public readonly record struct DatabaseEntry(DatabaseKey Key, DatabaseValue Value)
{
    public static DatabaseEntry Empty => new(DatabaseKey.Empty, DatabaseValue.Empty);

    public bool IsEmpty => Key.IsEmpty && Value.IsEmpty;
}

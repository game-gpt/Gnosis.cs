namespace Gnosis.Database.Core;

public readonly record struct DatabaseEntry(DatabaseKey Key, DatabaseValue Value)
{
    public static DatabaseEntry Empty => new(DatabaseKey.Empty, DatabaseValue.Empty);

    public bool IsEmpty => Key.IsEmpty && Value.IsEmpty;

    public SolidDB.Core.SolidEntry ToSolidEntry() => new(Key, Value);

    public static DatabaseEntry FromSolidEntry(SolidDB.Core.SolidEntry entry) => new(entry.Key, entry.Value);
}

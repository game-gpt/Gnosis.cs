namespace Gnosis.Database.Core;

public readonly record struct DatabaseEntry(DatabaseKey Key, DatabaseValue Value)
{
    public static DatabaseEntry Empty => new(DatabaseKey.Empty, DatabaseValue.Empty);

    public bool IsEmpty => Key.IsEmpty && Value.IsEmpty;

    public LightDB.Core.LightEntry ToLightEntry() => new(Key, Value);

    public static DatabaseEntry FromLightEntry(LightDB.Core.LightEntry entry) => new(entry.Key, entry.Value);
}

namespace Gnosis.Core.ValueObjects;

public readonly record struct EntityId(Guid Value)
{
    public static EntityId New() => new(Guid.NewGuid());
    public static readonly EntityId Empty = new(Guid.Empty);
}

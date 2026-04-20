namespace Gnosis.Core.ValueObjects;

public readonly record struct RegionId(Guid Value)
{
    public static RegionId New() => new(Guid.NewGuid());
    public static readonly RegionId Empty = new(Guid.Empty);
}

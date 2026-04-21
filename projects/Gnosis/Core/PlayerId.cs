namespace Gnosis.Core;

public readonly record struct PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.NewGuid());
    public static readonly PlayerId Empty = new(Guid.Empty);
}

namespace Gnosis.Core;

public readonly record struct EntityId
{
    private static uint _nextIndex = 1;

    public uint Index { get; }
    public uint Generation { get; }

    public EntityId(uint index, uint generation)
    {
        Index = index;
        Generation = generation;
    }

    public static readonly EntityId Null = new(0, 0);

    public bool IsNull => Index == 0 && Generation == 0;

    public static EntityId New()
    {
        return new EntityId(Interlocked.Increment(ref _nextIndex), 0);
    }

    public override string ToString()
    {
        return $"Entity({Index}:{Generation})";
    }
}

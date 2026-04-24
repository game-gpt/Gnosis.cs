namespace Gnosis.Core;

public readonly record struct PlayerId
{
    private static long _nextId = 1;

    public long Value { get; }

    public PlayerId(long value)
    {
        Value = value;
    }

    public static readonly PlayerId Empty = new(0);

    public bool IsEmpty => Value == 0;

    public static PlayerId New()
    {
        return new PlayerId(Interlocked.Increment(ref _nextId));
    }

    public static PlayerId FromLong(long value) => new(value);

    public override string ToString()
    {
        return $"Player({Value})";
    }
}

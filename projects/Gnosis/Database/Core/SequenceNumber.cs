namespace Gnosis.Database.Core;

public readonly record struct SequenceNumber(ulong Value)
{
    public SequenceNumber Next => new(Value + 1);

    public static readonly SequenceNumber Zero = new(0);

    public static readonly SequenceNumber Invalid = new(ulong.MaxValue);
}

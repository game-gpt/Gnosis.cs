namespace Gnosis.Database.Core;

public readonly record struct SequenceNumber(ulong Value)
{
    public SequenceNumber Next => new(Value + 1);

    public static readonly SequenceNumber Zero = new(0);

    public static readonly SequenceNumber Invalid = new(ulong.MaxValue);

    public static implicit operator LightDB.Core.SequenceNumber(SequenceNumber sn) => new(sn.Value);

    public static implicit operator SequenceNumber(LightDB.Core.SequenceNumber sn) => new(sn.Value);
}

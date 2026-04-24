namespace Gnosis.Database.Core;

public readonly record struct TransactionId(ulong Value)
{
    public static TransactionId New() => new(Interlocked.Increment(ref _counter));

    public static readonly TransactionId Min = new(0);

    private static ulong _counter;

    public static implicit operator LightDB.Core.TransactionId(TransactionId id) => new(id.Value);

    public static implicit operator TransactionId(LightDB.Core.TransactionId id) => new(id.Value);
}

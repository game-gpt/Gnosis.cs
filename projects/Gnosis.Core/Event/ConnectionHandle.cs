namespace Gnosis.Core.Event;

public readonly record struct ConnectionHandle
{
    internal int Id { get; init; }
    internal int Index { get; init; }
    internal Action? DisconnectAction { get; init; }

    public void Disconnect()
    {
        DisconnectAction?.Invoke();
    }
}

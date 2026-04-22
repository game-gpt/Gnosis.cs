namespace Gnosis.Security.AntiCheat;

public interface IHoneypot
{
    string Name { get; }
    object FakeValue { get; }
    bool IsTriggered { get; }

    event Action? OnTriggered;
}

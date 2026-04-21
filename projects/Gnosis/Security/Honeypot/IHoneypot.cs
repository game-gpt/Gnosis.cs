namespace Gnosis.Security.Honeypot;

public interface IHoneypot
{
    string Name { get; }
    object FakeValue { get; }
    bool IsTriggered { get; }

    event Action? OnTriggered;
}

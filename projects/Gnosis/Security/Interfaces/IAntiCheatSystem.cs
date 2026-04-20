using Gnosis.Core.ValueObjects;

namespace Gnosis.Security.Interfaces;

public interface IAntiCheatSystem
{
    void Initialize();
    void Shutdown();
    
    void RegisterChecker(IIntegrityChecker checker);
    bool PerformCheck(string checkerName);
    
    bool IsCompromised { get; }
    
    event EventHandler<ViolationEventArgs>? OnViolation;
}

public record ViolationEventArgs(
    Enums.ViolationType Type,
    Enums.ViolationResponse Response,
    string Details,
    EntityId? EntityId
);

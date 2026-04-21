using Gnosis.ECS.Core;

namespace Gnosis.Security;

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
    ViolationType Type,
    ViolationResponse Response,
    string Details,
    EntityId? EntityId
);

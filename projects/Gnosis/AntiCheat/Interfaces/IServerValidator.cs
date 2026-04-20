using Gnosis.Core.ValueObjects;

namespace Gnosis.AntiCheat.Interfaces;

public interface IServerValidator
{
    bool ValidatePlayerAction(PlayerId playerId, string actionType, byte[] actionData);
    bool ValidatePosition(PlayerId playerId, float x, float y, float z, float timestamp);
    bool ValidateTransaction(PlayerId playerId, string transactionType, long amount);
    
    void RecordSuspiciousActivity(PlayerId playerId, Enums.ViolationType type, string details);
}

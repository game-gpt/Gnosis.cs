using Gnosis.Core;

namespace Gnosis.Security;

public interface IServerValidator
{
    bool ValidatePlayerAction(PlayerId playerId, string actionType, byte[] actionData);
    bool ValidatePosition(PlayerId playerId, float x, float y, float z, float timestamp);
    bool ValidateTransaction(PlayerId playerId, string transactionType, long amount);
    
    void RecordSuspiciousActivity(PlayerId playerId, ViolationType type, string details);
}

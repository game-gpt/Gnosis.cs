using Gnosis.Core.ValueObjects;

namespace Gnosis.Security.Interfaces;

public interface IRateLimiter
{
    bool IsAllowed(PlayerId playerId, string actionType);
    void RecordAction(PlayerId playerId, string actionType);
    void Reset(PlayerId playerId);
}

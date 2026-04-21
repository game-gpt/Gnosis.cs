using Gnosis.Core;

namespace Gnosis.Security;

public interface IRateLimiter
{
    bool IsAllowed(PlayerId playerId, string actionType);
    void RecordAction(PlayerId playerId, string actionType);
    void Reset(PlayerId playerId);
}

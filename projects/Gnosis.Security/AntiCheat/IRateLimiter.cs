using Gnosis.Core;
using Gnosis.Core.Event;

namespace Gnosis.Security.AntiCheat;

public interface IRateLimiter
{
    bool IsAllowed(PlayerId playerId, string actionType);
    void RecordAction(PlayerId playerId, string actionType);
    void Reset(PlayerId playerId);
}

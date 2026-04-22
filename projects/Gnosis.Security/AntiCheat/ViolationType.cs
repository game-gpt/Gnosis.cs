namespace Gnosis.Security.AntiCheat;

public enum ViolationType
{
    MemoryTampering,
    SpeedHack,
    IntegrityCheckFailed,
    HoneypotTriggered,
    ServerValidationFailed,
    RateLimitExceeded
}

namespace Gnosis.Security.Moderation;

public sealed record ModerationResult(ModerationStatus Status, string? Reason = null, float Confidence = 1.0f);

public enum ModerationStatus
{
    Approved,
    Rejected,
    NeedsReview
}

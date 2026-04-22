namespace Gnosis.Security.AntiCheat;

[AttributeUsage(AttributeTargets.Method)]
public class RateLimitAttribute : Attribute
{
    public int MaxCalls { get; set; }
    public double PerSeconds { get; set; }

    public RateLimitAttribute(int maxCalls, double perSeconds)
    {
        MaxCalls = maxCalls;
        PerSeconds = perSeconds;
    }
}

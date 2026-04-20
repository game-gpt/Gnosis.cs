namespace Gnosis.Core.ValueObjects;

public readonly record struct Timestamp(DateTimeOffset Value)
{
    public long UnixTimeSeconds => Value.ToUnixTimeSeconds();
    public long UnixTimeMilliseconds => Value.ToUnixTimeMilliseconds();

    public static Timestamp Now => new(DateTimeOffset.UtcNow);
    public static Timestamp FromUnixTimeSeconds(long seconds) => new(DateTimeOffset.FromUnixTimeSeconds(seconds));
    public static Timestamp FromUnixTimeMilliseconds(long milliseconds) => new(DateTimeOffset.FromUnixTimeMilliseconds(milliseconds));
}

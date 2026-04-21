namespace Gnosis.Infrastructure;

public interface ILogger
{
    void LogDebug(string message);
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogException(Exception exception, string? message = null);

    void LogDebug(string message, params (string Key, object Value)[] properties);
    void LogInfo(string message, params (string Key, object Value)[] properties);
    void LogWarning(string message, params (string Key, object Value)[] properties);
    void LogError(string message, params (string Key, object Value)[] properties);

    bool IsEnabled { get; }
    void SetLogLevel(LogLevel level);
}

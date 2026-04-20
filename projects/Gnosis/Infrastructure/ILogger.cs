namespace Gnosis.Infrastructure;

public interface ILogger
{
    void LogDebug(string message);
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogException(Exception exception, string? message = null);
    
    bool IsEnabled { get; }
    void SetLogLevel(LogLevel level);
}

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

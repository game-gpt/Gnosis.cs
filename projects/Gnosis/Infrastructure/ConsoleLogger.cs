using System.Linq;

namespace Gnosis.Infrastructure;

public class ConsoleLogger : ILogger
{
    public bool IsEnabled { get; private set; } = true;
    private LogLevel _level = LogLevel.Info;

    public void LogDebug(string message)
    {
        if (IsEnabled && _level <= LogLevel.Debug)
            Console.WriteLine($"[DEBUG] {message}");
    }

    public void LogInfo(string message)
    {
        if (IsEnabled && _level <= LogLevel.Info)
            Console.WriteLine($"[INFO] {message}");
    }

    public void LogWarning(string message)
    {
        if (IsEnabled && _level <= LogLevel.Warning)
            Console.WriteLine($"[WARN] {message}");
    }

    public void LogError(string message)
    {
        if (IsEnabled && _level <= LogLevel.Error)
            Console.WriteLine($"[ERROR] {message}");
    }

    public void LogException(Exception exception, string? message = null)
    {
        if (IsEnabled && _level <= LogLevel.Error)
        {
            if (message != null)
                Console.WriteLine($"[ERROR] {message}");
            Console.WriteLine($"[ERROR] {exception.GetType().Name}: {exception.Message}");
            Console.WriteLine(exception.StackTrace);
        }
    }

    public void LogDebug(string message, params (string Key, object Value)[] properties)
    {
        if (IsEnabled && _level <= LogLevel.Debug)
        {
            var formatted = FormatProperties(message, properties);
            Console.WriteLine($"[DEBUG] {formatted}");
        }
    }

    public void LogInfo(string message, params (string Key, object Value)[] properties)
    {
        if (IsEnabled && _level <= LogLevel.Info)
        {
            var formatted = FormatProperties(message, properties);
            Console.WriteLine($"[INFO] {formatted}");
        }
    }

    public void LogWarning(string message, params (string Key, object Value)[] properties)
    {
        if (IsEnabled && _level <= LogLevel.Warning)
        {
            var formatted = FormatProperties(message, properties);
            Console.WriteLine($"[WARN] {formatted}");
        }
    }

    public void LogError(string message, params (string Key, object Value)[] properties)
    {
        if (IsEnabled && _level <= LogLevel.Error)
        {
            var formatted = FormatProperties(message, properties);
            Console.WriteLine($"[ERROR] {formatted}");
        }
    }

    public void SetLogLevel(LogLevel level)
    {
        _level = level;
    }

    private static string FormatProperties(string message, (string Key, object Value)[] properties)
    {
        if (properties == null || properties.Length == 0)
        {
            return message;
        }

        var props = string.Join(", ", properties.Select(p => $"{p.Key}={p.Value}"));
        return $"{message} [{props}]";
    }
}

using System;
using Gnosis.Infrastructure.Interfaces;

namespace Gnosis.Infrastructure.Implementations;

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
    
    public void SetLogLevel(LogLevel level)
    {
        _level = level;
    }
}

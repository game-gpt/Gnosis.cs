using System.Linq;

namespace Gnosis.Infrastructure;

/// <summary>
/// 控制台日志器，将日志输出到控制台
/// </summary>
public class ConsoleLogger : ILogger
{
    #region 字段

    private LogLevel _level = LogLevel.Info;

    #endregion

    #region 属性

    /// <summary>
    /// 日志器是否启用
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    #endregion

    #region ILogger 实现

    /// <summary>
    /// 输出调试级别日志
    /// </summary>
    public void LogDebug(string message)
    {
        if (IsEnabled && _level <= LogLevel.Debug)
        {
            Console.WriteLine($"[DEBUG] {message}");
        }
    }

    /// <summary>
    /// 输出信息级别日志
    /// </summary>
    public void LogInfo(string message)
    {
        if (IsEnabled && _level <= LogLevel.Info)
        {
            Console.WriteLine($"[INFO] {message}");
        }
    }

    /// <summary>
    /// 输出警告级别日志
    /// </summary>
    public void LogWarning(string message)
    {
        if (IsEnabled && _level <= LogLevel.Warning)
        {
            Console.WriteLine($"[WARN] {message}");
        }
    }

    /// <summary>
    /// 输出错误级别日志
    /// </summary>
    public void LogError(string message)
    {
        if (IsEnabled && _level <= LogLevel.Error)
        {
            Console.WriteLine($"[ERROR] {message}");
        }
    }

    /// <summary>
    /// 输出异常日志
    /// </summary>
    public void LogException(Exception exception, string? message = null)
    {
        if (IsEnabled && _level <= LogLevel.Error)
        {
            if (message != null)
            {
                Console.WriteLine($"[ERROR] {message}");
            }
            Console.WriteLine($"[ERROR] {exception.GetType().Name}: {exception.Message}");
            Console.WriteLine(exception.StackTrace);
        }
    }

    /// <summary>
    /// 输出带属性的调试级别日志
    /// </summary>
    public void LogDebug(string message, params (string Key, object Value)[] properties)
    {
        if (IsEnabled && _level <= LogLevel.Debug)
        {
            Console.WriteLine($"[DEBUG] {FormatProperties(message, properties)}");
        }
    }

    /// <summary>
    /// 输出带属性的信息级别日志
    /// </summary>
    public void LogInfo(string message, params (string Key, object Value)[] properties)
    {
        if (IsEnabled && _level <= LogLevel.Info)
        {
            Console.WriteLine($"[INFO] {FormatProperties(message, properties)}");
        }
    }

    /// <summary>
    /// 输出带属性的警告级别日志
    /// </summary>
    public void LogWarning(string message, params (string Key, object Value)[] properties)
    {
        if (IsEnabled && _level <= LogLevel.Warning)
        {
            Console.WriteLine($"[WARN] {FormatProperties(message, properties)}");
        }
    }

    /// <summary>
    /// 输出带属性的错误级别日志
    /// </summary>
    public void LogError(string message, params (string Key, object Value)[] properties)
    {
        if (IsEnabled && _level <= LogLevel.Error)
        {
            Console.WriteLine($"[ERROR] {FormatProperties(message, properties)}");
        }
    }

    /// <summary>
    /// 设置日志级别
    /// </summary>
    public void SetLogLevel(LogLevel level)
    {
        _level = level;
    }

    #endregion

    #region 私有方法

    private static string FormatProperties(string message, (string Key, object Value)[] properties)
    {
        if (properties == null || properties.Length == 0)
        {
            return message;
        }

        var props = string.Join(", ", properties.Select(p => $"{p.Key}={p.Value}"));
        return $"{message} [{props}]";
    }

    #endregion
}
